using System;
using System.IO;
using System.Linq;
using System.Text;

namespace ProjectBrain
{
    [Serializable] public sealed class BrainEditView
    {
        public string taskId;
        public int revision;
        public string nodeId;
        public string type;
        public string path;
        public string content;
        public string summary;
        public string expectedHash;
        public string expectedContextHash;
        public bool writable;
    }
    [Serializable] public sealed class BrainEditReceipt
    {
        public int schemaVersion = 1;
        public string id;
        public string taskId;
        public int revision;
        public string nodeId;
        public string operation;
        public string actor = "agent";
        public string beforeHash;
        public string afterHash;
        public string state;
        public string createdUtc;
        public string recordPath;
        public bool needsAssetRefresh;
    }
    public sealed class BrainEditService
    {
        private const int MaxBytes = 131072;
        private readonly string root;
        private readonly IBrainJson json;
        private readonly Func<string, string> resolve;
        private readonly BrainWorkspace workspace;
        private readonly BrainStore store;
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
        public BrainEditService(string root, IBrainJson json, Func<string, string> resolve)
        { this.root = root; this.json = json; this.resolve = resolve; workspace = new BrainWorkspace(root); store = new BrainStore(Path.Combine(root, ".projectbrain"), json); }
        private BrainTaskRecord Task(string id, int revision)
        {
            var task = new BrainTaskService(root, json).Load();
            BrainWorkspace.Require(task != null && task.id == id && task.revision == revision, "작업 ID/revision 충돌. 다시 읽으세요."); return task;
        }
        private bool Allowed(BrainTaskRecord task, string path) => BrainWorkspace.InScope(path) && task.allowedPaths.Any(p => p.EndsWith("/", StringComparison.Ordinal) ? path.StartsWith(p, StringComparison.Ordinal) : path == p);
        private string CodePath(BrainNode node)
        {
            var path = resolve(node.assetGuid) ?? "";
            BrainWorkspace.Require(BrainWorkspace.InScope(path) && path.EndsWith(".cs", StringComparison.Ordinal) && File.Exists(workspace.Resolve(path)), "기존 프로젝트 C# 파일만 지원합니다."); return path;
        }
        private static void Content(string text) => BrainWorkspace.Require(text != null && !text.Contains("\0") && Utf8.GetByteCount(text) <= MaxBytes, "UTF-8 텍스트 128KiB 이하만 지원합니다.");
        public BrainEditView Read(string taskId, int expectedRevision, string nodeId)
        {
            var task = Task(taskId, expectedRevision); var graph = new BrainGraphService(store); var node = graph.Get(nodeId);
            var result = new BrainEditView { taskId = taskId, revision = expectedRevision, nodeId = nodeId, type = node.type, summary = node.summary, expectedContextHash = "" };
            if (node.type == "Code")
            {
                result.path = CodePath(node); var bytes = File.ReadAllBytes(workspace.Resolve(result.path));
                BrainWorkspace.Require(bytes.Length <= MaxBytes, "파일이 128KiB를 초과합니다.");
                result.content = Utf8.GetString(bytes); Content(result.content); result.expectedHash = BrainWorkspace.HashBytes(bytes); result.writable = Allowed(task, result.path);
            }
            else
            {
                BrainWorkspace.Require(node.type == "Document", "Code/Document만 편집 조회할 수 있습니다.");
                result.path = ".projectbrain/nodes/" + BrainStore.SafeId(nodeId) + ".json"; workspace.Resolve(result.path);
                Content(node.body); Content(node.summary); result.content = node.body; result.expectedHash = BrainWorkspace.Hash(json.Write(node));
                var basis = new BrainCompletionService(root, json, resolve).InspectDocument(nodeId);
                result.expectedContextHash = basis.snapshotHash;
                result.writable = basis.state != "missing-code" && basis.codePaths.Length > 0 && basis.codePaths.All(p => Allowed(task, p));
            }
            return result;
        }
        public BrainEditReceipt Apply(string taskId, int expectedRevision, string nodeId, string expectedHash, string content)
        {
            Content(content); var view = Read(taskId, expectedRevision, nodeId);
            BrainWorkspace.Require(view.type == "Code" && view.writable && view.expectedHash == expectedHash, "Code 종류·허용 범위·예상 해시가 맞지 않습니다. 다시 읽으세요.");
            var after = BrainWorkspace.Hash(content);
            return Write(view, "apply", after, () =>
            {
                // Recheck immediately before mutation; external editors are not an OS-level transaction participant.
                Task(taskId, expectedRevision);
                BrainWorkspace.Require(workspace.FileHash(view.path) == expectedHash, "쓰기 직전 파일이 변경됐습니다.");
                BrainStore.AtomicWrite(workspace.Resolve(view.path), content);
            });
        }
        public BrainEditReceipt UpdateDocument(string taskId, int expectedRevision, string nodeId, string expectedHash, string expectedContextHash, string summary, string body)
        {
            Content(summary); Content(body); var view = Read(taskId, expectedRevision, nodeId);
            BrainWorkspace.Require(view.type == "Document" && view.writable && view.expectedHash == expectedHash && view.expectedContextHash == expectedContextHash, "문서·근거 코드·관계·허용 범위가 바뀌었습니다. 다시 읽으세요.");
            var node = store.LoadNode(nodeId);
            if (node.summary == summary && node.body == body) return Write(view, "update_document", view.expectedHash, () => { });
            node.summary = summary; node.body = body; node.status = "unreviewed"; node.updatedUtc = DateTime.UtcNow.ToString("O");
            var after = BrainWorkspace.Hash(json.Write(node));
            return Write(view, "update_document", after, () =>
            {
                var now = Read(taskId, expectedRevision, nodeId);
                BrainWorkspace.Require(now.expectedHash == expectedHash && now.expectedContextHash == expectedContextHash && now.writable, "쓰기 직전 문서 맥락이 변경됐습니다.");
                store.SaveNode(node);
            });
        }
        private BrainEditReceipt Write(BrainEditView view, string operation, string after, Action write)
        {
            var id = Guid.NewGuid().ToString("D");
            var receipt = new BrainEditReceipt { id = id, taskId = view.taskId, revision = view.revision, nodeId = view.nodeId, operation = operation, beforeHash = view.expectedHash, afterHash = after, state = view.expectedHash == after ? "unchanged" : "prepared", createdUtc = DateTime.UtcNow.ToString("O"), recordPath = ".projectbrain/edits/" + id + ".json", needsAssetRefresh = operation == "apply" && view.expectedHash != after };
            if (receipt.state == "unchanged") { receipt.recordPath = ""; return receipt; }
            // Persist intent first. If mutation or final receipt fails, keep prepared; never claim success.
            BrainStore.AtomicWrite(workspace.Resolve(receipt.recordPath), json.Write(receipt));
            try
            {
                write(); receipt.state = "applied";
                BrainStore.AtomicWrite(workspace.Resolve(receipt.recordPath), json.Write(receipt)); return receipt;
            }
            catch (Exception e)
            {
                throw new IOException("편집 완료를 확인하지 못했습니다. 원본을 다시 읽고 prepared 기록의 전후 해시를 비교하세요: " + receipt.recordPath + " · " + e.Message, e);
            }
        }
    }
}
