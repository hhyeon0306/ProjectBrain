using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using com.IvanMurzak.Unity.MCP.Editor.API;

namespace ProjectBrain
{
    [Serializable] public sealed class BrainVerificationRecord
    {
        public int schemaVersion = 1;
        public string id;
        public string taskId;
        public string kind;
        public string state;
        public string snapshotHash;
        public string startedUtc;
        public string endedUtc = "";
        public string summary = "";
        public int total;
        public int passed;
        public int failed;
        public int skipped;
    }
    public sealed class BrainVerificationStore
    {
        private readonly string root;
        private readonly IBrainJson json;
        public BrainVerificationStore(string root, IBrainJson json) { this.root = root; this.json = json; }
        private string PathFor(string id)
        {
            BrainWorkspace.Require(Guid.TryParseExact(id, "D", out _), "검증 ID 오류입니다.");
            return Path.Combine(root, ".projectbrain", "evidence", id + ".json");
        }
        public string Snapshot()
        {
            var graph = new BrainGraphService(new BrainStore(Path.Combine(root, ".projectbrain"), json));
            var watched = new BrainWorkspace(root).Capture();
            // Generated evidence/activity links must not invalidate the code/document they describe.
            return BrainWorkspace.Hash(json.Write(watched) + "\n" + string.Join("\n", graph.Nodes.Values.Where(n => n.type != "Evidence" && n.type != "Activity").OrderBy(n => n.id, StringComparer.Ordinal).Select(n => json.Write(n))) + "\n" + string.Join("\n", graph.Relations.Where(Semantic).OrderBy(r => r.id, StringComparer.Ordinal).Select(r => json.Write(r))));
        }
        internal static bool Semantic(BrainRelation r) => r.type != "verified_by" && r.type != "worked_on_in";
        public BrainVerificationRecord Load(string id)
        {
            var record = json.Read<BrainVerificationRecord>(File.ReadAllText(PathFor(id)));
            BrainWorkspace.Require(record.schemaVersion == 1 && record.id == id && Guid.TryParseExact(record.taskId, "D", out _) && new[] { "compile", "editmode" }.Contains(record.kind) && new[] { "running", "passed", "failed", "interrupted", "stale", "timeout" }.Contains(record.state) && record.snapshotHash != null && System.Text.RegularExpressions.Regex.IsMatch(record.snapshotHash, "\\A[0-9a-f]{64}\\z") && DateTimeOffset.TryParse(record.startedUtc, out _) && (record.state == "running" || DateTimeOffset.TryParse(record.endedUtc, out _)) && record.total >= 0 && record.passed >= 0 && record.failed >= 0 && record.skipped >= 0, "손상된 검증 기록: " + id);
            return record;
        }
        public BrainVerificationRecord[] ForTask(string taskId)
        {
            var dir = Path.Combine(root, ".projectbrain", "evidence");
            return !Directory.Exists(dir) ? Array.Empty<BrainVerificationRecord>() : Directory.GetFiles(dir, "*.json").Select(p => Load(Path.GetFileNameWithoutExtension(p))).Where(r => r.taskId == taskId).OrderBy(r => r.startedUtc, StringComparer.Ordinal).ToArray();
        }
        internal BrainVerificationRecord Begin(string taskId, string kind)
        {
            BrainWorkspace.Require(kind == "compile" || kind == "editmode", "compile 또는 editmode만 지원합니다.");
            var record = new BrainVerificationRecord { id = Guid.NewGuid().ToString("D"), taskId = taskId, kind = kind, state = "running", snapshotHash = Snapshot(), startedUtc = DateTime.UtcNow.ToString("O") };
            BrainStore.AtomicWrite(PathFor(record.id), json.Write(record)); return record;
        }
        internal BrainVerificationRecord Finish(string id, string state, int total, int passed, int failed, int skipped, string summary)
        {
            var record = Load(id); BrainWorkspace.Require(record.state == "running", "종료된 검증은 덮어쓰지 않습니다.");
            record.state = state; record.total = total; record.passed = passed; record.failed = failed; record.skipped = skipped; record.summary = summary;
            if (state == "passed" && (failed != 0 || skipped != 0 || record.kind == "editmode" && (total == 0 || passed != total))) record.state = "failed";
            if (Snapshot() != record.snapshotHash) record.state = "stale";
            record.endedUtc = DateTime.UtcNow.ToString("O"); BrainStore.AtomicWrite(PathFor(id), json.Write(record));
            Publish(record); return record;
        }
        private void Publish(BrainVerificationRecord record)
        {
            var store = new BrainStore(Path.Combine(root, ".projectbrain"), json);
            var task = new BrainTaskService(root, json).Load();
            var node = new BrainNode { id = "evidence:" + record.id, type = "Evidence", title = record.kind + " · " + record.state, summary = record.summary, body = ".projectbrain/evidence/" + record.id + ".json", status = "recorded", updatedUtc = record.endedUtc };
            store.SaveNode(node);
            if (task == null || task.id != record.taskId) return;
            var graph = new BrainGraphService(store);
            var targets = task.targetNodeIds.Where(id => graph.Get(id).type == "Feature" || graph.Get(id).type == "Code");
            var links = targets.Select(id => new BrainRelation { id = "relation:" + BrainWorkspace.Hash(id + node.id), from = id, to = node.id, type = "verified_by", source = "brain-unity-runner", createdUtc = record.endedUtc });
            store.SaveRelations(graph.Relations.Concat(links));
        }
        public bool CurrentPass(BrainVerificationRecord record, string snapshot) => record != null && record.state == "passed" && record.snapshotHash == snapshot && record.failed == 0 && record.skipped == 0 && (record.kind == "compile" || record.total > 0 && record.passed == record.total);
    }
    [InitializeOnLoad]
    public static class BrainUnityVerification
    {
        private const string Pending = "Brain.Verification.Pending";
        private static string current;
        private static int assemblies;
        private static int errors;
        private static int cached;
        private static bool compileStarted;
        private static double nextTick;
        private static readonly Callback callback = new Callback();
        private static BrainVerificationStore Store => new BrainVerificationStore(ScriptDocumentService.ProjectRoot, new UnityBrainJson());
        static BrainUnityVerification()
        {
            current = SessionState.GetString(Pending, ""); if (current.Length == 0) current = null;
            assemblies = SessionState.GetInt(Pending + ".assemblies", 0); errors = SessionState.GetInt(Pending + ".errors", 0);
            cached = SessionState.GetInt(Pending + ".cached", 0);
            compileStarted = SessionState.GetBool(Pending + ".started", false);
            Tool_Tests.TestRunnerApi.RegisterCallbacks(callback);
            CompilationPipeline.compilationStarted += _ => { if (current != null && Store.Load(current).kind == "compile") { compileStarted = true; SessionState.SetBool(Pending + ".started", true); } };
            CompilationPipeline.assemblyCompilationFinished += (_, messages) => { if (current != null && compileStarted) { assemblies++; errors += messages.Count(m => m.type == CompilerMessageType.Error); SessionState.SetInt(Pending + ".assemblies", assemblies); SessionState.SetInt(Pending + ".errors", errors); } };
            CompilationPipeline.assemblyCompilationNotRequired += _ => { if (current != null && compileStarted) { cached++; SessionState.SetInt(Pending + ".cached", cached); } };
            CompilationPipeline.compilationFinished += _ => { if (current != null && compileStarted) End(errors == 0 && assemblies + cached > 0 ? "passed" : "failed", assemblies + cached, assemblies + cached - errors, errors, 0, "Unity compilation callbacks; compiled=" + assemblies + ", up-to-date=" + cached + ", errors=" + errors); };
            EditorApplication.delayCall += Recover;
            EditorApplication.update += Tick;
        }
        private static void Recover()
        {
            // A lost callback after reload/editor restart is not a successful run.
            foreach (var r in Store.ForTask(new BrainTaskService(ScriptDocumentService.ProjectRoot, new UnityBrainJson()).Load()?.id ?? "").Where(r => r.state == "running"))
                if (r.id != current) Store.Finish(r.id, "interrupted", 0, 0, 0, 0, "Editor/domain restarted before a terminal callback.");
            if (current == null) SessionState.EraseString(Pending);
            else if (!EditorApplication.isCompiling && !TestRunActive() && DateTimeOffset.UtcNow - DateTimeOffset.Parse(Store.Load(current).startedUtc) > TimeSpan.FromSeconds(10)) End("interrupted", 0, 0, 0, 0, "Runner no longer active and terminal callback was not observed.");
        }
        private static bool TestRunActive() { var method = typeof(TestRunnerApi).GetMethod("IsRunActive", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic); BrainWorkspace.Require(method != null, "설치된 Unity TestRunner 실행 상태 API를 확인할 수 없습니다."); return (bool)method.Invoke(null, null); }
        public static BrainVerificationRecord Start(string taskId, int expectedRevision, string kind)
        {
            var task = new BrainTaskService(ScriptDocumentService.ProjectRoot, new UnityBrainJson()).Load();
            BrainWorkspace.Require(task != null && task.id == taskId && task.revision == expectedRevision, "작업 ID/revision 충돌입니다.");
            BrainWorkspace.Require(current == null && !EditorApplication.isCompiling && !EditorApplication.isUpdating && !EditorApplication.isPlayingOrWillChangePlaymode && !TestRunActive(), "Editor 또는 검증 실행이 진행 중입니다.");
            for (int i = 0; i < EditorSceneManager.sceneCount; i++) BrainWorkspace.Require(!EditorSceneManager.GetSceneAt(i).isDirty, "미저장 씬을 보존합니다. 검증을 시작하지 않았습니다.");
            var record = Store.Begin(taskId, kind); current = record.id; SessionState.SetString(Pending, current);
            try
            {
                if (kind == "compile") { assemblies = errors = cached = 0; compileStarted = false; SessionState.SetBool(Pending + ".started", false); SessionState.SetInt(Pending + ".assemblies", 0); SessionState.SetInt(Pending + ".errors", 0); SessionState.SetInt(Pending + ".cached", 0); CompilationPipeline.RequestScriptCompilation(RequestScriptCompilationOptions.CleanBuildCache); }
                else { Tool_Tests.TestRunnerApi.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode })); }
            }
            catch (Exception e) { End("failed", 0, 0, 0, 0, e.Message); throw; }
            return record;
        }
        private static void Tick()
        {
            if (EditorApplication.timeSinceStartup < nextTick) return;
            nextTick = EditorApplication.timeSinceStartup + 1;
            if (current == null) return;
            Recover();
            if (current != null && DateTimeOffset.UtcNow - DateTimeOffset.Parse(Store.Load(current).startedUtc) > TimeSpan.FromMinutes(5)) End("timeout", 0, 0, 0, 0, "No terminal callback within five minutes; not a pass.");
        }
        private static void End(string state, int total, int passed, int failed, int skipped, string summary)
        {
            var id = current; if (id == null) return;
            try { Store.Finish(id, state, total, Math.Max(0, passed), failed, skipped, summary); }
            finally { current = null; compileStarted = false; SessionState.EraseString(Pending); }
        }
        private sealed class Callback : ICallbacks
        {
            public void RunStarted(ITestAdaptor tests) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                if (current == null || Store.Load(current).kind != "editmode") return;
                End(result.TestStatus.ToString() == "Passed" ? "passed" : "failed", result.PassCount + result.FailCount + result.SkipCount + result.InconclusiveCount, result.PassCount, result.FailCount, result.SkipCount + result.InconclusiveCount, "All discovered EditMode tests. " + result.TestStatus + "; " + result.Message);
            }
        }
    }
}
