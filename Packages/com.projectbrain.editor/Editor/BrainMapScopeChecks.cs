using System;
using System.IO;
using System.Linq;

namespace ProjectBrain
{
    public static class BrainMapScopeChecks
    {
        public static int RunInEditor()
        {
            var path = Path.Combine(Path.GetTempPath(), "BrainScopeChecks-" + Guid.NewGuid().ToString("N"));
            var store = new BrainStore(path, new UnityBrainJson()); int passed = 0;
            Action<bool, string> check = (ok, reason) => { if (!ok) throw new Exception(reason); passed++; };
            try
            {
                var ids = new[] { "domain:a", "domain:b", "feature:a/move", "feature:b/save", "asset:00000000000000000000000000000001", "asset:00000000000000000000000000000002", "document:design", "asset:00000000000000000000000000000003" };
                var types = new[] { "Domain", "Domain", "Feature", "Feature", "Code", "Code", "Document", "Image" };
                for (int i = 0; i < ids.Length; i++)
                {
                    var node = BrainStoreChecks.Node(ids[i], types[i]);
                    if (types[i] == "Code" || types[i] == "Image") node.assetGuid = ids[i].Substring(6);
                    store.SaveNode(node);
                }
                Func<int, int, string, BrainRelation> edge = (a, b, type) => new BrainRelation { id = "relation:" + a + "-" + b, from = ids[a], to = ids[b], type = type, source = "fixture", createdUtc = "2026-09-07T00:00:00Z" };
                store.SaveRelations(new[] { edge(0, 2, "contains"), edge(1, 3, "contains"), edge(2, 4, "implemented_by"), edge(3, 4, "implemented_by"), edge(3, 5, "implemented_by"), edge(4, 6, "documented_by"), edge(6, 7, "illustrated_by"), edge(4, 5, "depends_on"), edge(5, 4, "depends_on") });
                var before = Directory.GetFiles(path, "*.json", SearchOption.AllDirectories).ToDictionary(f => f, File.ReadAllText);
                var graph = new BrainGraphService(store);
                var scope = BrainMapScope.Collect(graph, ids[0]);
                check(scope.SetEquals(new[] { ids[0], ids[2], ids[4], ids[6], ids[7] }), "Domain owns feature, implementation, document and image without dependency flood");
                check(BrainMapScope.Collect(graph, ids[2]).SetEquals(scope.Where(id => id != ids[0])), "Feature scope excludes parent domain");
                check(BrainMapScope.Collect(graph, ids[1]).Contains(ids[4]), "Shared code remains accessible in both domains");
                var boundary = BrainMapScope.Boundary(graph, scope);
                check(boundary.Length == 3 && boundary.Any(r => r.from == ids[5] && r.to == ids[4]) && boundary.Any(r => r.from == ids[4] && r.to == ids[5]), "Boundary retains incoming and outgoing dependencies and shared ownership");
                check(BrainMapScope.Collect(graph, "").Count == ids.Length, "All scope includes disconnected domains");
                bool rejected = false; try { BrainMapScope.Collect(graph, ids[4]); } catch (ArgumentException) { rejected = true; }
                check(rejected, "Code cannot masquerade as domain");
                var map = new BrainMapView(graph, _ => { }); map.SetScope(scope);
                check(map.VisibleIds.ToHashSet().SetEquals(scope), "Map applies scope");
                map.ShowType("Document", false); map.SetQuery("design");
                check(map.VisibleCount == 0, "Scope composes with query and type filter");
                map.SetQuery(""); map.ShowType("Document", true); map.SetScope(null);
                check(map.VisibleCount == ids.Length, "All view restores nodes");
                check(before.All(p => File.ReadAllText(p.Key) == p.Value), "Navigation does not modify stored data");
                return passed;
            }
            finally { if (Directory.Exists(path)) Directory.Delete(path, true); }
        }
    }
}
