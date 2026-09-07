using System;
using System.Collections.Generic;
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
            Republish(record.id); return record;
        }
        // A terminal result is the durable publication intent. Never run the test again
        // or rewrite its outcome merely because a later graph write failed.
        public bool Republish(string id)
        {
            var record = Load(id);
            BrainWorkspace.Require(record.state != "running", "실행 중인 검증은 발행할 수 없습니다.");
            var store = new BrainStore(Path.Combine(root, ".projectbrain"), json);
            // Task targets are immutable; History also resolves tasks archived since the run.
            var task = new BrainTaskLifecycle(root, json, _ => "").History(record.taskId).task;
            var node = EvidenceNode(record);
            var previous = store.LoadNode(node.id);
            BrainWorkspace.Require(previous == null || json.Write(previous) == json.Write(node), "검증 노드가 원본과 다릅니다. 기존 기록을 보존합니다: " + id);
            bool changed = previous == null;
            if (previous == null) store.SaveNode(node);
            var graph = new BrainGraphService(store);
            var targets = task.targetNodeIds.Where(target => graph.Get(target).type == "Feature" || graph.Get(target).type == "Code");
            var links = targets.Select(target => new BrainRelation { id = "relation:" + BrainWorkspace.Hash(target + node.id), from = target, to = node.id, type = "verified_by", source = "brain-unity-runner", createdUtc = record.endedUtc });
            var relations = graph.Relations.ToList();
            foreach (var link in links)
            {
                var existing = relations.FirstOrDefault(r => r.id == link.id || r.from == link.from && r.to == link.to && r.type == link.type);
                BrainWorkspace.Require(existing == null || json.Write(existing) == json.Write(link), "검증 연결이 원본과 다릅니다. 기존 연결을 보존합니다: " + id);
                if (existing == null) { relations.Add(link); changed = true; }
            }
            if (relations.Count != graph.Relations.Length) store.SaveRelations(relations);
            return changed;
        }
        public bool IsPublished(BrainVerificationRecord record, BrainGraphService graph, string[] targets)
        {
            if (record == null || record.state == "running") return false;
            string id = "evidence:" + record.id;
            return graph.Nodes.TryGetValue(id, out var node) && json.Write(node) == json.Write(EvidenceNode(record))
                && targets.Where(t => graph.Get(t).type == "Feature" || graph.Get(t).type == "Code").All(t => graph.Relations.Any(r => r.id == "relation:" + BrainWorkspace.Hash(t + id) && r.from == t && r.to == id && r.type == "verified_by" && r.source == "brain-unity-runner" && r.createdUtc == record.endedUtc));
        }
        private static BrainNode EvidenceNode(BrainVerificationRecord record) => new BrainNode { id = "evidence:" + record.id, type = "Evidence", title = record.kind + " · " + record.state, summary = record.summary, body = ".projectbrain/evidence/" + record.id + ".json", status = "recorded", updatedUtc = record.endedUtc };
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
        private static double nextPublicationRetry;
        private static readonly Dictionary<string, int> publications = new Dictionary<string, int>();
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
            EditorApplication.delayCall += () => { Recover(); QueuePublications(); };
            EditorApplication.update += Tick;
        }
        private static void Recover()
        {
            // A lost callback after reload/editor restart is not a successful run.
            foreach (var r in Store.ForTask(new BrainTaskService(ScriptDocumentService.ProjectRoot, new UnityBrainJson()).Load()?.id ?? "").Where(r => r.state == "running"))
                if (r.id != current)
                {
                    try { Store.Finish(r.id, "interrupted", 0, 0, 0, 0, "Editor/domain restarted before a terminal callback."); }
                    catch { if (Store.Load(r.id).state == "running") throw; publications[r.id] = 0; }
                }
            if (current == null) SessionState.EraseString(Pending);
            else if (!EditorApplication.isCompiling && !TestRunActive() && DateTimeOffset.UtcNow - DateTimeOffset.Parse(Store.Load(current).startedUtc) > TimeSpan.FromSeconds(10)) End("interrupted", 0, 0, 0, 0, "Runner no longer active and terminal callback was not observed.");
        }
        private static void QueuePublications()
        {
            try
            {
                var task = new BrainTaskService(ScriptDocumentService.ProjectRoot, new UnityBrainJson()).Load();
                if (task == null) return;
                foreach (var record in Store.ForTask(task.id).Where(r => r.state != "running")) publications[record.id] = 0;
            }
            catch (Exception e) { Debug.LogWarning("[Project Brain] 검증 연결 복구 준비 실패. 원본 보존: " + e.Message); }
        }
        private static void RetryPublications()
        {
            if (EditorApplication.timeSinceStartup < nextPublicationRetry || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            nextPublicationRetry = EditorApplication.timeSinceStartup + 10;
            foreach (var id in publications.Keys.ToArray())
            {
                try { Store.Republish(id); publications.Remove(id); }
                catch (Exception e)
                {
                    if (++publications[id] < 5) continue;
                    publications.Remove(id);
                    Debug.LogWarning("[Project Brain] 검증 연결 복구 대기: " + id + " · 원본 결과 보존. 잠금/충돌 해결 후 Republish로 재시도하세요. " + e.Message);
                }
            }
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
            RetryPublications();
            if (current == null) return;
            Recover();
            if (current != null && DateTimeOffset.UtcNow - DateTimeOffset.Parse(Store.Load(current).startedUtc) > TimeSpan.FromMinutes(5)) End("timeout", 0, 0, 0, 0, "No terminal callback within five minutes; not a pass.");
        }
        private static void End(string state, int total, int passed, int failed, int skipped, string summary)
        {
            var id = current; if (id == null) return;
            try { Store.Finish(id, state, total, Math.Max(0, passed), failed, skipped, summary); }
            catch (Exception e)
            {
                // If the terminal result survived, only its projection needs retrying.
                if (Store.Load(id).state == "running") throw;
                publications[id] = 0;
                Debug.LogWarning("[Project Brain] 검증 결과는 저장됐고 그래프 연결은 재시도합니다: " + id + " · " + e.Message);
            }
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
