using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectBrain
{
    // Follow ownership, not arbitrary dependencies: shared/external code stays a boundary link.
    public static class BrainMapScope
    {
        public static HashSet<string> Collect(BrainGraphService graph, string id)
        {
            if (string.IsNullOrEmpty(id)) return new HashSet<string>(graph.Nodes.Keys);
            var root = graph.Get(id);
            if (root.type != "Domain" && root.type != "Feature") throw new ArgumentException("도메인 또는 기능을 선택하세요.");
            var result = new HashSet<string> { id };
            var queue = new Queue<string>(); queue.Enqueue(id);
            while (queue.Count > 0)
            {
                var next = queue.Dequeue();
                foreach (var edge in graph.Around(next).Where(r => r.from == next &&
                    (r.type == "contains" || r.type == "implemented_by" || r.type == "documented_by" || r.type == "illustrated_by" || r.type == "verified_by" || r.type == "worked_on_in" || r.type == "references")))
                    if (result.Add(edge.to)) queue.Enqueue(edge.to);
            }
            return result;
        }
        public static BrainRelation[] Boundary(BrainGraphService graph, HashSet<string> scope) => graph.Relations
            .Where(r => r.type != "contains" && scope.Contains(r.from) != scope.Contains(r.to))
            .OrderBy(r => r.id, StringComparer.Ordinal).ToArray();
    }
}
