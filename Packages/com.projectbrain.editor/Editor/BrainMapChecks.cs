using System;
using System.IO;
using System.Linq;

namespace ProjectBrain
{
    // Runs in the Editor through MCP. A fixture exercises cycles, disconnected nodes and pre-layout calls.
    public static class BrainMapChecks
    {
        public static int RunInEditor()
        {
            var path = Path.Combine(Path.GetTempPath(), "BrainMapChecks-" + Guid.NewGuid().ToString("N"));
            var json = new UnityBrainJson(); var store = new BrainStore(path, json); int passed = 0;
            Action<bool, string> check = (ok, reason) => { if (!ok) throw new Exception(reason); passed++; };
            try
            {
                var a = BrainStoreChecks.Node("document:a", "Document"); a.title = "이동 설계";
                var b = BrainStoreChecks.Node("document:b", "Document"); b.title = "Input";
                var c = BrainStoreChecks.Node("document:c", "Document");
                var d = BrainStoreChecks.Node("reference:detached", "Reference");
                foreach (var node in new[] { a, b, c, d }) store.SaveNode(node);
                store.SaveRelations(new[] {
                    new BrainRelation { id = "relation:ab", from = a.id, to = b.id, type = "depends_on", source = "fixture", createdUtc = "2026-09-07T00:00:00Z" },
                    new BrainRelation { id = "relation:bc", from = b.id, to = c.id, type = "depends_on", source = "fixture", createdUtc = "2026-09-07T00:00:00Z" },
                    new BrainRelation { id = "relation:ca", from = c.id, to = a.id, type = "depends_on", source = "fixture", createdUtc = "2026-09-07T00:00:00Z" }
                });
                var before = Directory.GetFiles(path, "*.json", SearchOption.AllDirectories).ToDictionary(f => f, File.ReadAllText);
                var graph = new BrainGraphService(store);
                var map = new BrainMapView(graph, _ => { });
                check(map.VisibleCount == 4, "Disconnected nodes remain visible");
                map.SetSelected(a.id); map.SetLocal(true);
                check(map.VisibleCount == 3 && !map.VisibleIds.Contains(d.id), "Two-hop traversal terminates on cycle and excludes disconnected node");
                map.SetQuery("INPUT");
                check(map.VisibleIds.SequenceEqual(new[] { b.id }), "Case-insensitive search");
                map.SetQuery("없는 항목"); map.Fit();
                check(map.VisibleCount == 0, "Empty result fit");
                map.SetQuery("이동");
                check(map.VisibleIds.SequenceEqual(new[] { a.id }), "Korean title search");
                map.SetQuery(""); map.ShowType("Document", false);
                check(map.VisibleCount == 0 && map.Selected == a.id, "Filter preserves selection");
                map.SetLocal(false);
                check(map.VisibleIds.SequenceEqual(new[] { d.id }), "Type filter composes with scope");
                map.ShowType("Document", true); map.Fit();
                check(map.VisibleCount == 4 && map.Positions.Values.All(p => !float.IsNaN(p.x) && !float.IsNaN(p.y) && !float.IsInfinity(p.x) && !float.IsInfinity(p.y)) && !float.IsNaN(map.Zoom), "Fit before panel layout cannot poison positions");
                var other = new BrainMapView(graph, _ => { });
                check(map.Positions.All(p => other.Positions[p.Key] == p.Value), "Deterministic initial layout");
                check(before.All(p => File.ReadAllText(p.Key) == p.Value), "Navigation preserves stored node and relation bytes");
                var empty = new BrainMapView(new BrainGraphService(new BrainStore(Path.Combine(path, "empty"), json)), _ => { }); empty.Fit();
                check(empty.VisibleCount == 0, "Empty graph supported");
                return passed;
            }
            finally { if (Directory.Exists(path)) Directory.Delete(path, true); }
        }
    }
}
