using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ProjectBrain
{
    [Serializable] public sealed class BrainReviewRecord
    {
        public int schemaVersion = 1;
        public string taskId;
        public string documentId;
        public string snapshotHash;
        public string actor = "human-ui";
        public string reviewedUtc;
    }
    [Serializable] public sealed class BrainDocumentReview
    {
        public string documentId;
        public string state;
        public string snapshotHash;
        public string[] codePaths;
    }
    [Serializable] public sealed class BrainCompletionReason
    {
        public string code;
        public string target;
        public string nextAction;
    }
    [Serializable] public sealed class BrainCompletionResult
    {
        public string taskId;
        public int revision;
        public bool completed = false;
        public bool ready;
        public string policy = "v1-compile-editmode-human-review";
        public BrainVerificationRecord[] verification;
        public string activityId = "";
        public BrainDocumentReview[] documents;
        public BrainCompletionReason[] reasons;
    }
    [Serializable] public sealed class BrainCompletionActivity
    {
        public int schemaVersion = 1;
        public string id;
        public string taskId;
        public int revision;
        public string snapshotHash;
        public string[] verificationIds;
        public string completedUtc;
    }
    public sealed class BrainCompletionService
    {
        private readonly string root;
        private readonly IBrainJson json;
        private readonly Func<string, string> resolve;
        private readonly BrainWorkspace workspace;
        public BrainCompletionService(string root, IBrainJson json, Func<string, string> resolve)
        { this.root = root; this.json = json; this.resolve = resolve; workspace = new BrainWorkspace(root); }
        private BrainGraphService Graph() => new BrainGraphService(new BrainStore(Path.Combine(root, ".projectbrain"), json));
        private string ReviewPath(string taskId, string documentId) => workspace.Resolve(".projectbrain/reviews/" + BrainWorkspace.Hash(taskId + "\n" + documentId) + ".json");
        private BrainReviewRecord Load(string taskId, string documentId)
        {
            var path = ReviewPath(taskId, documentId);
            if (!File.Exists(path)) return null;
            var record = json.Read<BrainReviewRecord>(File.ReadAllText(path));
            BrainWorkspace.Require(record.schemaVersion == 1 && record.taskId == taskId && record.documentId == documentId && record.actor == "human-ui" && DateTimeOffset.TryParse(record.reviewedUtc, out _) && record.snapshotHash != null && System.Text.RegularExpressions.Regex.IsMatch(record.snapshotHash, "\\A[0-9a-f]{64}\\z"), "손상된 문서 확인 기록: " + path);
            return record;
        }
        private BrainDocumentReview Inspect(BrainGraphService graph, string taskId, string documentId)
        {
            var doc = graph.Get(documentId);
            BrainWorkspace.Require(doc.type == "Document", "문서만 확인할 수 있습니다.");
            var owners = graph.Relations.Where(r => r.type == "documented_by" && r.to == documentId).Select(r => r.from).ToHashSet();
            var codes = owners.Where(id => graph.Get(id).type == "Code").Concat(graph.Relations.Where(r => r.type == "implemented_by" && owners.Contains(r.from)).Select(r => r.to)).Distinct().OrderBy(id => id, StringComparer.Ordinal).ToArray();
            var stamps = codes.Select(id => { var path = resolve(graph.Get(id).assetGuid) ?? ""; return new BrainCodeBasis { nodeId = id, path = path, sha256 = path.Length == 0 ? "" : workspace.FileHash(path) }; }).ToArray();
            // Relations and Code node identities are part of the reviewed context; removing an edge invalidates it.
            var payload = json.Write(doc) + "\n" + string.Join("\n", graph.Relations.Where(BrainVerificationStore.Semantic).OrderBy(r => r.id, StringComparer.Ordinal).Select(r => json.Write(r))) + "\n" + string.Join("\n", codes.Select(id => json.Write(graph.Get(id)))) + "\n" + string.Join("\n", stamps.Select(s => json.Write(s)));
            var hash = BrainWorkspace.Hash(payload);
            var record = Load(taskId, documentId);
            return new BrainDocumentReview { documentId = documentId, snapshotHash = hash, codePaths = stamps.Select(s => s.path).ToArray(), state = stamps.Length == 0 || stamps.Any(s => s.sha256.Length == 0) ? "missing-code" : record == null ? "unreviewed" : record.snapshotHash == hash ? "current" : "stale" };
        }
        public BrainDocumentReview InspectDocument(string documentId, string displayedDocumentPayload = "")
        {
            var task = new BrainTaskService(root, json).Load();
            BrainWorkspace.Require(task != null, "활성 작업이 없습니다.");
            var graph = Graph();
            BrainWorkspace.Require(displayedDocumentPayload.Length == 0 || json.Write(graph.Get(documentId)) == displayedDocumentPayload, "화면의 문서가 오래됐습니다. 자료를 새로 읽으세요.");
            return Inspect(graph, task.id, documentId);
        }
        // No MCP approval endpoint. This records an explicit UI assertion, not authenticated identity.
        internal void ConfirmFromHuman(string taskId, int expectedRevision, string documentId, string expectedSnapshot)
        {
            var task = new BrainTaskService(root, json).Load();
            BrainWorkspace.Require(task != null && task.id == taskId && task.revision == expectedRevision, "작업이 바뀌었습니다. 다시 읽고 확인하세요.");
            var review = Inspect(Graph(), taskId, documentId);
            BrainWorkspace.Require(review.state != "missing-code" && review.snapshotHash == expectedSnapshot, "문서·코드·관계가 바뀌었거나 없습니다. 다시 읽고 확인하세요.");
            BrainStore.AtomicWrite(ReviewPath(taskId, documentId), json.Write(new BrainReviewRecord { taskId = taskId, documentId = documentId, snapshotHash = expectedSnapshot, reviewedUtc = DateTime.UtcNow.ToString("O") }));
        }
        public BrainCompletionResult Check(string taskId = "", int expectedRevision = 0)
        {
            var status = new BrainTaskService(root, json).Status(); var task = status.task; var graph = Graph();
            BrainWorkspace.Require(taskId.Length == 0 || task.id == taskId && task.revision == expectedRevision, "작업 ID/revision 충돌. 최신 작업을 다시 읽으세요.");
            var reasons = new List<BrainCompletionReason>();
            Action<string, string, string> reason = (code, target, next) => reasons.Add(new BrainCompletionReason { code = code, target = target, nextAction = next });
            var scope = new HashSet<string>(task.targetNodeIds);
            var queue = new Queue<string>(scope);
            while (queue.Count > 0)
            {
                var id = queue.Dequeue();
                foreach (var edge in graph.Relations.Where(r => r.from == id && (r.type == "contains" || r.type == "implemented_by"))) if (scope.Add(edge.to)) queue.Enqueue(edge.to);
            }
            foreach (var change in status.changes)
            {
                if (!change.allowed) reason("outside-allowed", change.path, "허용 범위 밖 변경을 별도로 검토하세요. 자동 승인하지 않습니다.");
                var mapped = graph.Nodes.Values.Where(n => n.type == "Code" && (resolve(n.assetGuid) == change.path || n.lastKnownPath == change.path)).ToArray();
                foreach (var node in mapped) scope.Add(node.id);
                if (mapped.Length == 0) reason("unmapped-change", change.path, "이 변경의 문서·검증 범위를 명시적으로 연결해야 합니다.");
            }
            // Include owning Features of changed/target Code and their sibling Code contracts.
            foreach (var edge in graph.Relations.Where(r => r.type == "implemented_by" && scope.Contains(r.to)).ToArray()) scope.Add(edge.from);
            foreach (var edge in graph.Relations.Where(r => r.type == "implemented_by" && scope.Contains(r.from)).ToArray()) scope.Add(edge.to);
            var documents = new HashSet<string>(scope.Where(id => graph.Get(id).type == "Document"));
            foreach (var id in scope.Where(id => graph.Get(id).type == "Code" || graph.Get(id).type == "Feature").OrderBy(id => id, StringComparer.Ordinal))
            {
                var links = graph.Relations.Where(r => r.from == id && r.type == "documented_by").ToArray();
                foreach (var link in links) documents.Add(link.to);
                if (graph.Get(id).type == "Code" && links.Length == 0) reason("missing-document", id, "Code에 필수 설명 문서를 연결하고 사람이 확인하세요.");
            }
            if (documents.Count == 0) reason("missing-document", task.id, "확인할 필수 문서가 없습니다. 대상 코드와 문서를 연결하세요.");
            var reviews = documents.OrderBy(id => id, StringComparer.Ordinal).Select(id => Inspect(graph, task.id, id)).ToArray();
            foreach (var review in reviews.Where(r => r.state != "current")) reason("document-" + review.state, review.documentId, "Explorer에서 현재 문서와 연결 코드를 읽고 사람이 확인하세요.");
            foreach (var limitation in status.coverageLimitations) reason("coverage-limited", limitation, "감시하지 못한 범위를 해결해야 합니다.");
            var verificationStore = new BrainVerificationStore(root, json);
            var records = verificationStore.ForTask(task.id);
            var snapshot = verificationStore.Snapshot();
            foreach (var kind in new[] { "compile", "editmode" })
            {
                var latest = records.LastOrDefault(r => r.kind == kind);
                if (!verificationStore.CurrentPass(latest, snapshot)) reason("verification-" + kind, task.id, "현재 파일 기준 " + kind + " 검증이 필요합니다. brain_verify로 실행하세요.");
            }
            return new BrainCompletionResult { taskId = task.id, revision = task.revision, documents = reviews, verification = records, ready = reasons.Count == 0, reasons = reasons.ToArray() };
        }
        public BrainCompletionResult Complete(string taskId, int expectedRevision)
        {
            var before = new BrainVerificationStore(root, json).Snapshot();
            var result = Check(taskId, expectedRevision);
            if (!result.ready) return result;
            BrainWorkspace.Require(before == new BrainVerificationStore(root, json).Snapshot(), "완료 검사 중 파일이 바뀌었습니다.");
            var activity = new BrainCompletionActivity { id = Guid.NewGuid().ToString("D"), taskId = taskId, revision = expectedRevision, snapshotHash = before, verificationIds = new[] { "compile", "editmode" }.Select(k => result.verification.Last(r => r.kind == k).id).ToArray(), completedUtc = DateTime.UtcNow.ToString("O") };
            var path = ".projectbrain/activities/" + activity.id + ".json";
            BrainStore.AtomicWrite(Path.Combine(root, path), json.Write(activity));
            var store = new BrainStore(Path.Combine(root, ".projectbrain"), json);
            var node = new BrainNode { id = "activity:" + activity.id, type = "Activity", title = "작업 완료 기록", summary = "현재 문서 확인·컴파일·EditMode 정책 충족", body = path, status = "recorded", updatedUtc = activity.completedUtc };
            store.SaveNode(node);
            var graph = new BrainGraphService(store);
            var task = new BrainTaskService(root, json).Load();
            store.SaveRelations(graph.Relations.Concat(task.targetNodeIds.Where(id => graph.Get(id).type == "Feature" || graph.Get(id).type == "Code").Select(id => new BrainRelation { id = "relation:" + BrainWorkspace.Hash(id + node.id), from = id, to = node.id, type = "worked_on_in", source = "brain-completion", createdUtc = activity.completedUtc })));
            result.completed = true; result.activityId = node.id; return result;
        }
    }
}
