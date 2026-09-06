using System;
using System.IO;
using System.Linq;

namespace ProjectBrain
{
    [Serializable] public sealed class BrainTaskRecord
    {
        public int schemaVersion = 1;
        public string id;
        public int revision = 1;
        public string purpose;
        public string[] targetNodeIds;
        public string progress = "";
        public string decisions = "";
        public string unresolved = "";
        public string nextAction = "";
        public string[] references = Array.Empty<string>();
        public string[] allowedPaths;
        public BrainScopeChange[] scopeChanges = Array.Empty<BrainScopeChange>();
        public BrainSnapshot baseline;
        public string gitStateAtBegin = "not-queried; baseline includes pre-existing working files";
        public string createdUtc;
        public string updatedUtc;
    }
    [Serializable] public sealed class BrainTaskStatus
    {
        public BrainTaskRecord task;
        public bool resumed;
        public BrainChange[] changes;
        public string[] coverageLimitations;
    }
    public sealed class BrainTaskService
    {
        private readonly BrainWorkspace workspace;
        private readonly BrainStore store;
        private readonly IBrainJson json;
        private readonly string path;
        public BrainTaskService(string projectRoot, IBrainJson json)
        {
            workspace = new BrainWorkspace(projectRoot); this.json = json;
            store = new BrainStore(Path.Combine(projectRoot, ".projectbrain"), json);
            path = Path.Combine(projectRoot, ".projectbrain", "tasks", "active.json");
        }
        internal void Validate(BrainTaskRecord task)
        {
            BrainWorkspace.Require(task != null && task.schemaVersion == 1 && Guid.TryParseExact(task.id, "D", out _) && task.revision > 0 && !string.IsNullOrWhiteSpace(task.purpose), "손상된 작업 기록입니다.");
            BrainWorkspace.Require(task.targetNodeIds != null && task.targetNodeIds.Length > 0 && task.references != null && task.baseline != null && task.baseline.files != null && task.baseline.limitations != null, "작업 필드 누락입니다.");
            BrainWorkspace.Require(task.progress != null && task.decisions != null && task.unresolved != null && task.nextAction != null && task.gitStateAtBegin != null, "작업 요약 누락입니다.");
            BrainWorkspace.Require(DateTimeOffset.TryParse(task.createdUtc, out _) && DateTimeOffset.TryParse(task.updatedUtc, out _), "작업 시각 오류입니다.");
            workspace.ValidateAllowed(task.allowedPaths);
            task.scopeChanges = task.scopeChanges ?? Array.Empty<BrainScopeChange>();
            int previousRevision = 0;
            foreach (var change in task.scopeChanges)
            {
                BrainWorkspace.Require(change.revision > previousRevision && change.revision <= task.revision && !string.IsNullOrWhiteSpace(change.reason) && DateTimeOffset.TryParse(change.changedUtc, out _), "범위 변경 이력 오류입니다.");
                workspace.ValidateAllowed(change.before); workspace.ValidateAllowed(change.after); previousRevision = change.revision;
            }
            var graph = new BrainGraphService(store);
            foreach (var id in task.targetNodeIds.Concat(task.references)) graph.Get(id);
            BrainWorkspace.Require(task.baseline.files.Select(f => f.path).Distinct(StringComparer.Ordinal).Count() == task.baseline.files.Length, "기준선 경로 중복입니다.");
            foreach (var f in task.baseline.files)
            {
                BrainWorkspace.Require(BrainWorkspace.InScope(f.path) && f.sha256 != null && System.Text.RegularExpressions.Regex.IsMatch(f.sha256, "\\A[0-9a-f]{64}\\z"), "기준선 오류입니다.");
                // Baseline paths can now be missing or linked; current coverage reports that separately.
                BrainWorkspace.Require(!f.path.Contains("\\") && !f.path.Split('/').Any(p => p == ".." || p == "." || p == ""), "기준선 경로 오류입니다.");
            }
        }
        public BrainTaskRecord Load()
        {
            if (!File.Exists(path)) return null;
            try { var task = json.Read<BrainTaskRecord>(File.ReadAllText(path)); Validate(task); return new BrainTaskLifecycle(workspace.Root, json, _ => "").IsClosed(task) ? null : task; }
            catch (InvalidDataException e) { throw new InvalidDataException(path + ": " + e.Message, e); }
        }
        public BrainTaskStatus Begin(string purpose, string[] targets, string[] allowedPaths, string taskId = "")
        {
            var active = Load();
            if (active != null)
            {
                BrainWorkspace.Require(string.IsNullOrEmpty(taskId) || taskId == active.id, "활성 작업 ID 충돌: " + active.id);
                return Status(active, true);
            }
            BrainWorkspace.Require(string.IsNullOrEmpty(taskId), "재개할 활성 작업이 없습니다.");
            var now = DateTime.UtcNow.ToString("O");
            var task = new BrainTaskRecord { id = Guid.NewGuid().ToString("D"), purpose = purpose, targetNodeIds = targets, allowedPaths = allowedPaths, createdUtc = now, updatedUtc = now, baseline = new BrainSnapshot { files = Array.Empty<BrainFileStamp>(), limitations = Array.Empty<string>() } };
            Validate(task);
            task.baseline = workspace.Capture();
            Validate(task);
            BrainStore.AtomicWrite(path, json.Write(task));
            return new BrainTaskStatus { task = task, resumed = false, changes = Array.Empty<BrainChange>(), coverageLimitations = task.baseline.limitations };
        }
        public BrainTaskRecord Update(string taskId, int expectedRevision, string progress, string decisions, string unresolved, string nextAction, string[] references)
        {
            var task = Load();
            BrainWorkspace.Require(task != null && task.id == taskId && task.revision == expectedRevision, "작업 ID/revision 충돌. 최신 작업을 다시 읽으세요.");
            task.progress = progress; task.decisions = decisions; task.unresolved = unresolved; task.nextAction = nextAction; task.references = references;
            task.revision = checked(task.revision + 1); task.updatedUtc = DateTime.UtcNow.ToString("O");
            Validate(task);
            BrainStore.AtomicWrite(path, json.Write(task));
            return task;
        }
        public BrainTaskStatus Status()
        {
            var task = Load(); BrainWorkspace.Require(task != null, "활성 작업이 없습니다."); return Status(task, true);
        }
        private BrainTaskStatus Status(BrainTaskRecord task, bool resumed)
        {
            var current = workspace.Capture();
            return new BrainTaskStatus { task = task, resumed = resumed, changes = BrainWorkspace.Changes(task.baseline, current, task.allowedPaths), coverageLimitations = task.baseline.limitations.Concat(current.limitations).Distinct().ToArray() };
        }
    }
}
