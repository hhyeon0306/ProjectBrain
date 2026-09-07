using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectBrain
{
    [Serializable] public sealed class BrainContextNode
    {
        public string id;
        public string type;
        public string title;
        public string summary;
        public string reference;
        public string assetPath;
        public BrainFreshness freshness;
    }
    [Serializable] public sealed class BrainContextEdge { public BrainRelation relation; public BrainFreshness freshness; }
    [Serializable] public sealed class BrainContextResult
    {
        public string rootNodeId;
        public BrainContextNode[] nodes;
        public BrainContextEdge[] relations;
        public int omittedByDepth;
        public int omittedByNodes;
        public int omittedByChars;
        public int omittedByHistory;
        public string[] nextNodeIds;
        public string counting = "Only observed omissions; unvisited totals unknown. Body/image/log bytes excluded. current is not review or test approval.";
        public string selection = "Semantic nodes first; at most two newest Evidence/Activity neighbors. Query omitted IDs for details.";
        public int chars;
    }
    public sealed class BrainContextService
    {
        private readonly BrainGraphService graph;
        private readonly BrainFreshnessService freshness;
        private readonly IBrainJson json;
        public BrainContextService(BrainGraphService graph, BrainFreshnessService freshness, IBrainJson json) { this.graph = graph; this.freshness = freshness; this.json = json; }
        public string Read(string rootNodeId, int depth = 2, int maxNodes = 12, int maxChars = 8000)
        {
            BrainWorkspace.Require(depth >= 0 && depth <= 2 && maxNodes >= 1 && maxNodes <= 30 && maxChars >= 1000 && maxChars <= 20000, "context 예산 범위 오류입니다.");
            graph.Get(rootNodeId);
            var ids = new List<string> { rootNodeId };
            var visited = new HashSet<string>(StringComparer.Ordinal) { rootNodeId };
            var depthOmissions = new HashSet<string>(StringComparer.Ordinal);
            var nodeOmissions = new HashSet<string>(StringComparer.Ordinal);
            var edges = new Dictionary<string, BrainRelation>(StringComparer.Ordinal);
            var history = new HashSet<string>(StringComparer.Ordinal);
            var frontier = new List<string> { rootNodeId };
            for (int level = 0; level <= depth && frontier.Count > 0; level++)
            {
                var nextFrontier = new List<string>();
                var layer = frontier.SelectMany(id => graph.Around(id).Select(r => (id, relation: r)))
                    .OrderBy(x => Array.IndexOf(BrainGraphService.RelationOrder, x.relation.type))
                    .ThenBy(x => x.relation.id, StringComparer.Ordinal).ThenBy(x => x.id, StringComparer.Ordinal);
                foreach (var pair in layer)
                {
                    var r = pair.relation;
                    var next = r.from == pair.id ? r.to : r.from;
                    if (visited.Contains(next)) { if (level < depth) edges[r.id] = r; continue; }
                    if (level == depth) { depthOmissions.Add(next); continue; }
                    if (IsHistory(next)) { history.Add(next); continue; }
                    if (ids.Count == maxNodes) { nodeOmissions.Add(next); continue; }
                    visited.Add(next); ids.Add(next); edges[r.id] = r; nextFrontier.Add(next);
                }
                frontier = nextFrontier;
            }
            // Records never consume the queue/budget needed to reach two-hop code documents.
            var orderedHistory = history.OrderByDescending(id => graph.Get(id).updatedUtc, StringComparer.Ordinal).ThenBy(id => id, StringComparer.Ordinal).ToArray();
            int historyCount = 0, omittedHistory = 0;
            foreach (var id in orderedHistory)
            {
                if (historyCount == 2) { omittedHistory++; continue; }
                if (ids.Count == maxNodes) { nodeOmissions.Add(id); continue; }
                ids.Add(id); visited.Add(id); historyCount++;
            }
            // Include only real edges between returned nodes; records are terminal neighbors.
            foreach (var edge in graph.Relations.Where(r => visited.Contains(r.from) && visited.Contains(r.to))) edges[edge.id] = edge;
            depthOmissions.ExceptWith(visited); depthOmissions.ExceptWith(history); nodeOmissions.ExceptWith(visited); depthOmissions.ExceptWith(nodeOmissions);
            var result = new BrainContextResult
            {
                rootNodeId = rootNodeId,
                nodes = ids.Select(id => { var n = graph.Get(id); return new BrainContextNode { id = id, type = n.type, title = Clip(n.title, 100), summary = Clip(n.summary, 240), reference = ".projectbrain/nodes/" + BrainStore.SafeId(id) + ".json", assetPath = freshness.AssetPath(n), freshness = freshness.Inspect(graph, id) }; }).ToArray(),
                relations = edges.Values.Select(r => new BrainContextEdge { relation = r, freshness = freshness.Inspect(graph, r.id) }).ToArray(),
                omittedByDepth = depthOmissions.Count, omittedByNodes = nodeOmissions.Count,
                omittedByHistory = omittedHistory,
                omittedByChars = ids.Count(id => graph.Get(id).title.Length > 100 || graph.Get(id).summary.Length > 240),
                nextNodeIds = nodeOmissions.Concat(depthOmissions).Concat(orderedHistory.Where(id => !visited.Contains(id))).Distinct().OrderBy(id => IsHistory(id) ? 1 : 0).ThenBy(id => id, StringComparer.Ordinal).Take(5).ToArray()
            };
            while (Encode(result).Length + BrainWire.EnvelopeChars > maxChars)
            {
                if (result.nodes.Length > 1)
                {
                    var removed = result.nodes.Last().id;
                    result.nodes = result.nodes.Take(result.nodes.Length - 1).ToArray();
                    result.relations = result.relations.Where(e => e.relation.from != removed && e.relation.to != removed).ToArray();
                    result.omittedByChars++;
                    result.nextNodeIds = new[] { removed }.Concat(result.nextNodeIds).Distinct().Take(5).ToArray();
                }
                else if (result.relations.Length > 0) { result.relations = result.relations.Take(result.relations.Length - 1).ToArray(); result.omittedByChars++; }
                else if (result.nextNodeIds.Length > 0) result.nextNodeIds = result.nextNodeIds.Take(result.nextNodeIds.Length - 1).ToArray();
                else if (result.nodes[0].summary.Length > 0 || result.nodes[0].title.Length > 0)
                { result.nodes[0].summary = ""; result.nodes[0].title = ""; result.omittedByChars++; }
                else throw new System.IO.InvalidDataException("budget-too-small: 루트 식별 정보와 메타데이터를 담을 수 없습니다.");
            }
            return Encode(result);
        }
        private bool IsHistory(string id) => graph.Get(id).type == "Evidence" || graph.Get(id).type == "Activity";
        private string Encode(BrainContextResult result)
        {
            string text;
            do { text = BrainWire.Encode(result); if (result.chars == text.Length + BrainWire.EnvelopeChars) return text; result.chars = text.Length + BrainWire.EnvelopeChars; } while (true);
        }
        private static string Clip(string value, int size) => value.Length <= size ? value : value.Substring(0, char.IsHighSurrogate(value[size - 1]) ? size - 1 : size) + "…";
    }
}
