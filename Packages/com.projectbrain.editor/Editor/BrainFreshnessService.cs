using System;
using System.IO;
using System.Linq;

namespace ProjectBrain
{
    [Serializable] public sealed class BrainCodeBasis { public string nodeId; public string path; public string sha256; }
    [Serializable] public sealed class BrainFreshnessRecord
    {
        public int schemaVersion = 1;
        public string targetId;
        public string payloadHash;
        public BrainCodeBasis[] codes;
        public string recordedUtc;
    }
    [Serializable] public sealed class BrainFreshness
    {
        public string state;
        public string recordPath;
        public string payloadHash;
        public int sourceCount;
    }
    public sealed class BrainFreshnessService
    {
        private readonly BrainWorkspace workspace;
        private readonly IBrainJson json;
        private readonly Func<string, string> resolveAsset;
        public BrainFreshnessService(string root, IBrainJson json, Func<string, string> resolveAsset)
        { workspace = new BrainWorkspace(root); this.json = json; this.resolveAsset = resolveAsset; }
        private string RecordPath(string id) => ".projectbrain/freshness/" + BrainStore.SafeId(id) + ".json";
        public string AssetPath(BrainNode node) => string.IsNullOrEmpty(node.assetGuid) ? "" : resolveAsset(node.assetGuid) ?? "";
        private string Payload(BrainGraphService graph, string id)
        {
            if (graph.Nodes.TryGetValue(id, out var node)) return json.Write(node);
            var relation = graph.Relations.SingleOrDefault(r => r.id == id);
            BrainWorkspace.Require(relation != null, "최신성 대상이 없습니다: " + id);
            return json.Write(relation);
        }
        private BrainFreshnessRecord Load(string id)
        {
            var path = workspace.Resolve(RecordPath(id));
            if (!File.Exists(path)) return null;
            var record = json.Read<BrainFreshnessRecord>(File.ReadAllText(path));
            BrainWorkspace.Require(record.schemaVersion == 1 && record.targetId == id && record.codes.Length > 0 && record.codes.Length <= 16 && record.codes.Select(c => c.nodeId).Distinct().Count() == record.codes.Length && DateTimeOffset.TryParse(record.recordedUtc, out _), "손상된 최신성 근거: " + path);
            BrainWorkspace.Require(IsHash(record.payloadHash) && record.codes.All(c => !string.IsNullOrEmpty(c.nodeId) && !string.IsNullOrEmpty(c.path) && IsHash(c.sha256)), "손상된 최신성 해시: " + path);
            return record;
        }
        private static bool IsHash(string value) => value != null && System.Text.RegularExpressions.Regex.IsMatch(value, "\\A[0-9a-f]{64}\\z");
        public BrainFreshnessRecord Record(BrainGraphService graph, string targetId, string[] codeNodeIds)
        {
            BrainWorkspace.Require(codeNodeIds != null && codeNodeIds.Length > 0 && codeNodeIds.Length <= 16 && codeNodeIds.Distinct().Count() == codeNodeIds.Length, "근거 Code ID 1~16개를 중복 없이 지정하세요.");
            Load(targetId); // Do not overwrite a corrupt record.
            var record = new BrainFreshnessRecord
            {
                targetId = targetId, payloadHash = BrainWorkspace.Hash(Payload(graph, targetId)), recordedUtc = DateTime.UtcNow.ToString("O"),
                codes = codeNodeIds.OrderBy(id => id, StringComparer.Ordinal).Select(id =>
                {
                    var code = graph.Get(id);
                    BrainWorkspace.Require(code.type == "Code", "근거는 Code 노드여야 합니다: " + id);
                    var path = resolveAsset(code.assetGuid);
                    BrainWorkspace.Require(!string.IsNullOrEmpty(path) && BrainWorkspace.InScope(path), "코드 자산이 없습니다: " + id);
                    var hash = workspace.FileHash(path);
                    BrainWorkspace.Require(hash.Length > 0, "코드 파일이 없습니다: " + path);
                    return new BrainCodeBasis { nodeId = id, path = path, sha256 = hash };
                }).ToArray()
            };
            BrainStore.AtomicWrite(workspace.Resolve(RecordPath(targetId)), json.Write(record));
            return record;
        }
        public BrainFreshness Inspect(BrainGraphService graph, string id)
        {
            var payload = Payload(graph, id);
            var record = Load(id);
            if (record == null) return new BrainFreshness { state = "unknown", recordPath = "", payloadHash = "", sourceCount = 0 };
            var state = BrainWorkspace.Hash(payload) == record.payloadHash ? "current" : "stale";
            foreach (var basis in record.codes)
            {
                if (!graph.Nodes.TryGetValue(basis.nodeId, out var code) || code.type != "Code") { state = "missing"; break; }
                var path = resolveAsset(code.assetGuid);
                if (string.IsNullOrEmpty(path)) { state = "missing"; break; }
                var hash = workspace.FileHash(path);
                if (hash.Length == 0) { state = "missing"; break; }
                if (path != basis.path || hash != basis.sha256) state = "stale";
            }
            return new BrainFreshness { state = state, recordPath = RecordPath(id), payloadHash = record.payloadHash, sourceCount = record.codes.Length };
        }
    }
}
