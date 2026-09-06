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
        [Description("Read the active task, watched changes, coverage and fixed completion policy reasons including current human document review. V1 validation is unavailable; never reports completion success.")]
        public static BrainStatusView Status() => MainThread.Instance.Run(() =>
        {
            var result = BrainStatusView.From(new BrainTaskService(ScriptDocumentService.ProjectRoot, new UnityBrainJson()).Status());
            result.completion = CompletionService().Check(result.task.id, result.task.revision);
            return result;
        });

        private static BrainCompletionService CompletionService() => new BrainCompletionService(ScriptDocumentService.ProjectRoot, new UnityBrainJson(), AssetDatabase.GUIDToAssetPath);

        [AiTool("brain_complete", Title = "Brain / Check Completion", ReadOnlyHint = true)]
        [Description("Recheck current task ID/revision, required human document reviews, watched changes and coverage. Returns completed=false with actionable reasons. V1 real validation is not implemented, so success is impossible. Does not alter/archive the task or approve documents; no policy override parameters.")]
        public static BrainCompletionResult Complete(string taskId, int expectedRevision) => MainThread.Instance.Run(() =>
        {
            BrainWorkspace.Require(!string.IsNullOrEmpty(taskId) && expectedRevision > 0, "작업 ID와 revision이 필요합니다.");
            return CompletionService().Check(taskId, expectedRevision);
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
