using System;
using System.IO;
using System.Linq;

namespace ProjectBrain
{
    [Serializable] public sealed class BrainScopeChange
    {
        public int revision;
        public string[] before;
        public string[] after;
        public string reason;
        public string changedUtc;
    }
    [Serializable] public sealed class BrainTaskArchive
    {
        public int schemaVersion = 1;
        public BrainTaskRecord task;
        public string taskHash;
        public string disposition;
        public string reason;
        public string closedUtc;
        public BrainChange[] changes;
        public string[] coverageLimitations;
        public BrainCompletionResult completion;
    }
    [Serializable] public sealed class BrainTaskHistoryView
    {
        public BrainTaskView task;
        public string disposition;
        public string reason;
        public string closedUtc;
        public string archivePath;
        public BrainScopeChange[] scopeChanges;
        public BrainCompletionReason[] remainingReasons;
    }
    public sealed class BrainTaskLifecycle
    {
        private readonly string root;
        private readonly IBrainJson json;
        private readonly Func<string, string> resolve;
        private readonly BrainWorkspace workspace;
        public BrainTaskLifecycle(string root, IBrainJson json, Func<string, string> resolve)
        { this.root = root; this.json = json; this.resolve = resolve; workspace = new BrainWorkspace(root); }
        private string ArchivePath(string id)
        {
            BrainWorkspace.Require(Guid.TryParseExact(id, "D", out _), "작업 ID 형식 오류입니다.");
            return ".projectbrain/tasks/archive/" + id + ".json";
        }
        private BrainTaskArchive ReadArchive(string id)
        {
            var path = workspace.Resolve(ArchivePath(id));
            if (!File.Exists(path)) return null;
            var a = json.Read<BrainTaskArchive>(File.ReadAllText(path));
            BrainWorkspace.Require(a.schemaVersion == 1 && a.task != null && a.task.id == id && a.taskHash == BrainWorkspace.Hash(json.Write(a.task)) && (a.disposition == "completed" || a.disposition == "abandoned") && !string.IsNullOrWhiteSpace(a.reason) && DateTimeOffset.TryParse(a.closedUtc, out _) && a.changes != null && a.coverageLimitations != null && a.completion != null && a.completion.taskId == id && a.completion.revision == a.task.revision && (a.disposition != "completed" || a.completion.completed && a.completion.ready && !string.IsNullOrEmpty(a.completion.activityId)), "손상된 종료 기록: " + path);
            return a;
        }
        internal bool IsClosed(BrainTaskRecord task)
        {
            var archive = ReadArchive(task.id);
            if (archive == null) return false;
            BrainWorkspace.Require(archive.taskHash == BrainWorkspace.Hash(json.Write(task)), "종료 기록과 active 사본이 다릅니다. 자동으로 덮어쓰지 않습니다.");
            return true;
        }
        private BrainTaskRecord RequireActive(string id, int revision)
        {
            var task = new BrainTaskService(root, json).Load();
            BrainWorkspace.Require(task != null && task.id == id && task.revision == revision, "작업 ID/revision 충돌. 최신 작업을 다시 읽으세요.");
            BrainWorkspace.Require(!new BrainVerificationStore(root, json).ForTask(id).Any(r => r.state == "running"), "검증 진행 중에는 범위 변경/종료할 수 없습니다.");
            return task;
        }
        private void Recheck(BrainTaskRecord before)
        {
            BrainWorkspace.Require(json.Write(RequireActive(before.id, before.revision)) == json.Write(before), "저장 직전 작업이 바뀌었습니다.");
        }
        public BrainTaskView SetScope(string taskId, int expectedRevision, string[] allowedPaths, string reason)
        {
            BrainWorkspace.Require(!string.IsNullOrWhiteSpace(reason), "범위 변경 이유가 필요합니다.");
            workspace.ValidateAllowed(allowedPaths);
            var before = RequireActive(taskId, expectedRevision);
            if (before.allowedPaths.SequenceEqual(allowedPaths)) return BrainTaskView.From(before);
            var task = json.Read<BrainTaskRecord>(json.Write(before));
            task.revision = checked(task.revision + 1); task.updatedUtc = DateTime.UtcNow.ToString("O");
            task.scopeChanges = before.scopeChanges.Concat(new[] { new BrainScopeChange { revision = task.revision, before = before.allowedPaths, after = allowedPaths, reason = reason, changedUtc = task.updatedUtc } }).ToArray();
            task.allowedPaths = allowedPaths;
            new BrainTaskService(root, json).Validate(task); Recheck(before);
            // Scope, reason and history commit with the active record in one file. Baseline never resets.
            BrainStore.AtomicWrite(workspace.Resolve(".projectbrain/tasks/active.json"), json.Write(task));
            return BrainTaskView.From(task);
        }
        public BrainTaskHistoryView Close(string taskId, int expectedRevision, string disposition, string reason)
        {
            BrainWorkspace.Require(disposition == "completed" || disposition == "abandoned", "completed 또는 abandoned를 명시하세요.");
            BrainWorkspace.Require(!string.IsNullOrWhiteSpace(reason), "종료 이유가 필요합니다.");
            var existing = ReadArchive(taskId);
            if (existing != null)
            {
                BrainWorkspace.Require(existing.task.revision == expectedRevision && existing.disposition == disposition && existing.reason == reason, "이미 종료된 작업의 다른 요청입니다.");
                return View(existing);
            }
            var task = RequireActive(taskId, expectedRevision);
            var status = new BrainTaskService(root, json).Status();
            var verification = new BrainVerificationStore(root, json); var snapshot = verification.Snapshot();
            var completionService = new BrainCompletionService(root, json, resolve);
            var completion = completionService.Check(taskId, expectedRevision);
            if (disposition == "completed")
            {
                BrainWorkspace.Require(completion.ready, "완료 조건을 충족하지 못했습니다. brain_status의 이유를 확인하세요.");
                completion = completionService.Complete(taskId, expectedRevision);
                BrainWorkspace.Require(completion.completed, "완료 기록을 저장하지 못했습니다.");
            }
            Recheck(task);
            BrainWorkspace.Require(snapshot == verification.Snapshot(), "종료 검사 중 코드/문서가 바뀌었습니다.");
            var archive = new BrainTaskArchive { task = task, taskHash = BrainWorkspace.Hash(json.Write(task)), disposition = disposition, reason = reason, closedUtc = DateTime.UtcNow.ToString("O"), changes = status.changes, coverageLimitations = status.coverageLimitations, completion = completion };
            var path = workspace.Resolve(ArchivePath(taskId));
            BrainWorkspace.Require(!File.Exists(path), "종료 기록은 덮어쓰지 않습니다.");
            // Archive is the single close marker. Keep active bytes for recovery; Begin replaces only a valid closed copy.
            BrainStore.AtomicWrite(path, json.Write(archive)); return View(archive);
        }
        private BrainTaskHistoryView View(BrainTaskArchive archive)
        {
            var view = BrainTaskView.From(archive.task); var path = ArchivePath(archive.task.id);
            view.baselineReference = path + "#task.baseline";
            return new BrainTaskHistoryView { task = view, disposition = archive.disposition, reason = archive.reason, closedUtc = archive.closedUtc, archivePath = path, scopeChanges = archive.task.scopeChanges ?? Array.Empty<BrainScopeChange>(), remainingReasons = archive.completion.reasons };
        }
        public BrainTaskHistoryView History(string taskId)
        {
            var archive = ReadArchive(taskId); if (archive != null) return View(archive);
            var task = new BrainTaskService(root, json).Load();
            BrainWorkspace.Require(task != null && task.id == taskId, "작업 기록을 찾을 수 없습니다.");
            return new BrainTaskHistoryView { task = BrainTaskView.From(task), disposition = "active", reason = "", closedUtc = "", archivePath = "", scopeChanges = task.scopeChanges, remainingReasons = Array.Empty<BrainCompletionReason>() };
        }
    }
}
