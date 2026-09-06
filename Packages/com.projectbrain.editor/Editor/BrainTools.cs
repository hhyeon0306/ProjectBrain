using System.ComponentModel;
using System.IO;
using System.Text.Json;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using UnityEditor;

namespace ProjectBrain
{
    [AiToolType]
    public sealed class BrainTools
    {
        [AiTool("brain_begin", Title = "Brain / Begin or Resume", ReadOnlyHint = false)]
        [Description("Create the first active Brain task or resume the existing task without replacing its purpose/baseline. An explicit different taskId is rejected. Returns current changes and coverage limitations; no completion approval.")]
        public static BrainStatusView Begin(string purpose = "", string[] targetNodeIds = null, string[] allowedPaths = null, string taskId = "") => MainThread.Instance.Run(() => BrainStatusView.From(new BrainTaskService(ScriptDocumentService.ProjectRoot, new UnityBrainJson()).Begin(purpose, targetNodeIds, allowedPaths, taskId)));

        [AiTool("brain_update_task", Title = "Brain / Update Task", ReadOnlyHint = false)]
        [Description("Persist progress, decisions/reasons, unresolved issues and next action with expectedRevision. References are existing Brain node IDs. Does not update baseline, freshness or approval.")]
        public static BrainTaskView UpdateTask(string taskId, int expectedRevision, string progress, string decisions, string unresolved, string nextAction, string[] references) => MainThread.Instance.Run(() => BrainTaskView.From(new BrainTaskService(ScriptDocumentService.ProjectRoot, new UnityBrainJson()).Update(taskId, expectedRevision, progress, decisions, unresolved, nextAction, references)));

        [AiTool("brain_status", Title = "Brain / Status", ReadOnlyHint = true)]
        [Description("Read the active task, watched changes, coverage and fixed completion policy reasons including current human document review. Includes real verification records and readiness; status itself never records completion.")]
        public static BrainStatusView Status() => MainThread.Instance.Run(() =>
        {
            var result = BrainStatusView.From(new BrainTaskService(ScriptDocumentService.ProjectRoot, new UnityBrainJson()).Status());
            result.completion = CompletionService().Check(result.task.id, result.task.revision);
            return result;
        });

        [AiTool("brain_set_scope", Title = "Brain / Set Task Scope", ReadOnlyHint = false)]
        [Description("Explicitly amend allowed paths with a reason and expected revision. Caller must have user authorization for the scope. Keeps original baseline/targets, stores before/after scope history atomically, and never approves documents or unmapped changes. Refuses running verification.")]
        public static BrainTaskView SetScope(string taskId, int expectedRevision, string[] allowedPaths, string reason) => MainThread.Instance.Run(() => { RequireEditIdle(); return Lifecycle().SetScope(taskId, expectedRevision, allowedPaths, reason); });

        [AiTool("brain_close_task", Title = "Brain / Close Task", ReadOnlyHint = false)]
        [Description("Explicitly close as completed (rechecks fixed policy and records Activity) or abandoned (unfinished, preserves blockers; no success). Reason/revision required. Archive preserves task/baseline/changes; next brain_begin may create a new task. Same exact close retry is idempotent. Never auto-abandon a blocked task to hide its failures.")]
        public static BrainTaskHistoryView CloseTask(string taskId, int expectedRevision, string disposition, string reason) => MainThread.Instance.Run(() => { RequireEditIdle(); return Lifecycle().Close(taskId, expectedRevision, disposition, reason); });

        [AiTool("brain_task_history", Title = "Brain / Task History", ReadOnlyHint = true)]
        [Description("Read compact active/archived task summary and scope changes by task ID. Archived blockers are historical, not current verification. Full baseline and closure evidence remain at archivePath; no reopen or mutation.")]
        public static BrainTaskHistoryView TaskHistory(string taskId) => MainThread.Instance.Run(() => Lifecycle().History(taskId));

        private static BrainTaskLifecycle Lifecycle() => new BrainTaskLifecycle(ScriptDocumentService.ProjectRoot, new UnityBrainJson(), AssetDatabase.GUIDToAssetPath);
        private static BrainCompletionService CompletionService() => new BrainCompletionService(ScriptDocumentService.ProjectRoot, new UnityBrainJson(), AssetDatabase.GUIDToAssetPath);
        private static BrainEditService EditService() => new BrainEditService(ScriptDocumentService.ProjectRoot, new UnityBrainJson(), AssetDatabase.GUIDToAssetPath);
        private static void RequireEditIdle() => BrainWorkspace.Require(!EditorApplication.isCompiling && !EditorApplication.isUpdating && !EditorApplication.isPlayingOrWillChangePlaymode, "Editor가 처리 중입니다.");

        [AiTool("brain_read_edit", Title = "Brain / Read Edit Target", ReadOnlyHint = true)]
        [Description("Read one existing Code or Document with task revision, expectedHash and document expectedContextHash. UTF-8 up to 128KiB; Code content preserves BOM as a character. writable reflects task allowed paths. Document data is nodes, not legacy docs. No human approval.")]
        public static BrainEditView ReadEdit(string taskId, int expectedRevision, string nodeId) => MainThread.Instance.Run(() => EditService().Read(taskId, expectedRevision, nodeId));

        [AiTool("brain_apply", Title = "Brain / Apply Code", ReadOnlyHint = false)]
        [Description("Replace one existing permitted C# Code node file using expected byte SHA256 and task revision. UTF-8 only; preserve returned BOM/newlines. No create/delete/rename/meta/policy change. Returns journal receipt; call assets-refresh afterwards if needsAssetRefresh. A prepared receipt after failure is an uncertain partial operation, not success.")]
        public static BrainEditReceipt Apply(string taskId, int expectedRevision, string nodeId, string expectedHash, string content) => MainThread.Instance.Run(() => { RequireEditIdle(); return EditService().Apply(taskId, expectedRevision, nodeId, expectedHash, content); });

        [AiTool("brain_update_document", Title = "Brain / Update Document", ReadOnlyHint = false)]
        [Description("Update an existing nodes Document summary/body using expected node payload hash, document/code/relation context hash and task revision from brain_read_edit. Every linked Code path must be allowed. Marks changed content unreviewed; never updates human review or freshness basis. Legacy docs are not synchronized. Returns journal receipt.")]
        public static BrainEditReceipt UpdateDocument(string taskId, int expectedRevision, string nodeId, string expectedHash, string expectedContextHash, string summary, string body) => MainThread.Instance.Run(() => { RequireEditIdle(); return EditService().UpdateDocument(taskId, expectedRevision, nodeId, expectedHash, expectedContextHash, summary, body); });

        [AiTool("brain_verify", Title = "Brain / Run Unity Verification", ReadOnlyHint = false)]
        [Description("Start real Unity compile or all discovered EditMode tests via the shared Ivan TestRunner API. Returns run ID immediately; poll brain_status for terminal records. Fixed kind compile/editmode; no caller supplied success. Refuses dirty scenes, busy editor and concurrent tests. Five minute timeout, interrupted reloads and changed snapshots are not passes.")]
        public static BrainVerificationRecord Verify(string taskId, int expectedRevision, string kind) => MainThread.Instance.Run(() => BrainUnityVerification.Start(taskId, expectedRevision, kind));

        [AiTool("brain_complete", Title = "Brain / Complete Current Snapshot", ReadOnlyHint = false)]
        [Description("Recheck task revision, required human document reviews, watched changes, coverage, and current compile/EditMode records. On success append an Activity for this snapshot; otherwise return reasons without approval. Keeps active task/baseline for inspection; no automatic archival or policy override.")]
        public static BrainCompletionResult Complete(string taskId, int expectedRevision) => MainThread.Instance.Run(() =>
        {
            BrainWorkspace.Require(!string.IsNullOrEmpty(taskId) && expectedRevision > 0, "작업 ID와 revision이 필요합니다.");
            BrainWorkspace.Require(!EditorApplication.isCompiling && !EditorApplication.isUpdating && !EditorApplication.isPlayingOrWillChangePlaymode, "Editor가 처리 중입니다.");
            return CompletionService().Complete(taskId, expectedRevision);
        });

        [AiTool("brain_context", Title = "Brain / Context", ReadOnlyHint = true)]
        [Description("Return deterministic bidirectional graph context as JSON text with a UTF-16 character budget including payload metadata. No full bodies/images/logs. Freshness current means byte agreement, not review or tests. Observed omissions and follow-up IDs are included.")]
        public static BrainContextResult Context(string rootNodeId, int depth = 1, int maxNodes = 12, int maxChars = 8000) => MainThread.Instance.Run(() =>
        {
            var json = new UnityBrainJson(); var root = ScriptDocumentService.ProjectRoot;
            return json.Read<BrainContextResult>(new BrainContextService(new BrainGraphService(new BrainStore(Path.Combine(root, ".projectbrain"), json)), new BrainFreshnessService(root, json, AssetDatabase.GUIDToAssetPath), json).Read(rootNodeId, depth, maxNodes, maxChars));
        });

        [AiTool("brain_record_basis", Title = "Brain / Record Freshness Basis", ReadOnlyHint = false)]
        [Description("Explicitly record the current node/relation payload and specified Code file hashes as a freshness basis. This is a byte baseline only, never human review, tests or completion approval. Do not refresh stale records merely to hide changes.")]
        public static BrainFreshnessRecord RecordBasis(string targetId, string[] codeNodeIds) => MainThread.Instance.Run(() =>
        {
            var json = new UnityBrainJson(); var root = ScriptDocumentService.ProjectRoot;
            return new BrainFreshnessService(root, json, AssetDatabase.GUIDToAssetPath).Record(new BrainGraphService(new BrainStore(Path.Combine(root, ".projectbrain"), json)), targetId, codeNodeIds);
        });
    }
}
