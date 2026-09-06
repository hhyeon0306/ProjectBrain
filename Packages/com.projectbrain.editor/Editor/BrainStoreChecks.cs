using System;
using System.IO;
using System.Linq;

namespace ProjectBrain
{
    public static class BrainStoreChecks
    {
        public static int RunInEditor()
        {
            var root = Path.Combine(Path.GetTempPath(), "BrainStoreChecks-" + Guid.NewGuid().ToString("N"));
            var codec = new UnityBrainJson();
            var store = new BrainStore(root, codec);
            int passed = 0;
            Action<bool, string> check = (condition, label) => { if (!condition) throw new Exception(label); passed++; };
            var domain = Node("domain:player", "Domain");
            domain.summary = "플레이어";
            domain.body = "한글\n설계";
            domain.tags = new[] { "게임" };
            store.SaveNode(domain);
            store.SaveNode(Node("feature:player/movement", "Feature"));
            store = new BrainStore(root, codec);
            var restored = store.LoadNode(domain.id);
            check(restored.body == domain.body && restored.tags.SequenceEqual(domain.tags) && restored.summary == domain.summary, "Unicode roundtrip");
            check(store.LoadNodes().Count == 2, "List persisted nodes");
            domain.title = "수정";
            store.SaveNode(domain);
            check(store.LoadNode(domain.id).title == "수정", "Atomic replacement");
            var edge = new BrainRelation { id = "relation:movement", from = domain.id, to = "feature:player/movement", type = "contains", source = "manual", createdUtc = domain.updatedUtc };
            store.SaveRelations(new[] { edge });
            check(new BrainStore(root, codec).RelationsFor(edge.to).Single().from == domain.id && store.RelationsFor(domain.id).Single().to == edge.to, "Bidirectional traversal preserves direction");
            var relationPath = Path.Combine(root, "relations.json");
            var relationBytes = File.ReadAllText(relationPath);
            Reject(() => store.SaveRelations(new[] { edge, edge })); passed++;
            edge.to = "feature:missing";
            Reject(() => store.SaveRelations(new[] { edge })); passed++;
            check(File.ReadAllText(relationPath) == relationBytes, "Invalid reference leaves relations unchanged");
            edge.to = "feature:player/movement";
            edge.type = "unknown";
            Reject(() => store.SaveRelations(new[] { edge })); passed++;
            edge.type = "contains";
            File.WriteAllText(relationPath, "not json");
            Reject(() => store.SaveRelations(new[] { edge })); passed++;
            check(File.ReadAllText(relationPath) == "not json", "Corrupt relations preserved");
            File.WriteAllText(relationPath, relationBytes);
            Reject(() => store.LoadNode("../escape")); passed++;
            var nodePath = Path.Combine(root, "nodes", BrainStore.SafeId(domain.id) + ".json");
            var nodeBytes = File.ReadAllText(nodePath);
            File.WriteAllText(nodePath, "not json");
            Reject(() => store.LoadNodes()); passed++;
            Reject(() => store.SaveNode(domain)); passed++;
            check(File.ReadAllText(nodePath) == "not json", "Corrupt node preserved");
            File.WriteAllText(nodePath, nodeBytes.Replace("\"schemaVersion\": 1", "\"schemaVersion\": 99"));
            Reject(() => store.LoadNode(domain.id)); passed++;
            Reject(() => store.SaveNode(domain)); passed++;
            File.WriteAllText(nodePath, nodeBytes);
            domain.status = "passed";
            Reject(() => store.SaveNode(domain)); passed++;
            domain.status = "unreviewed";
            var evidence = Node("evidence:12345678-1234-1234-1234-123456789abc", "Evidence");
            store.SaveNode(evidence);
            Reject(() => store.SaveNode(evidence)); passed++;
            var code = Node("asset:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "Code");
            code.assetGuid = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            code.lastKnownPath = "Assets/Player.cs";
            store.SaveNode(code);
            code.assetGuid = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
            Reject(() => store.SaveNode(code)); passed++;
            File.WriteAllText(nodePath, nodeBytes.Replace("domain:player", "domain:other"));
            Reject(() => store.LoadNodes()); passed++;
            File.WriteAllText(nodePath, nodeBytes);
            var additional = new[]
            {
                Node("project:game", "Project"), Node("document:movement", "Document"),
                Node("reference:unity-manual", "Reference"),
                Node("activity:12345678-1234-1234-1234-123456789abc", "Activity"),
                Node("asset:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb", "Image")
            };
            additional.Last().assetGuid = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
            foreach (var node in additional) store.SaveNode(node);
            check(store.LoadNodes().Select(n => n.type).Distinct().Count() == 9, "All nine node types survive persistence");
            Reject(() => store.SaveNode(additional[3])); passed++;
            Reject(() => store.SaveNode(Node("evidence:not-a-uuid", "Evidence"))); passed++;
            Reject(() => store.SaveNode(Node("feature:no-domain", "Feature"))); passed++;
            check(Directory.GetFiles(root, "*.tmp", SearchOption.AllDirectories).Length == 0, "No temporary files after writes");
            return passed;
        }

        internal static BrainNode Node(string id, string type) => new BrainNode { id = id, type = type, title = id, updatedUtc = "2026-09-06T00:00:00Z" };
        internal static void Reject(Action action)
        {
            try { action(); }
            catch (InvalidDataException) { return; }
            throw new Exception("Expected InvalidDataException");
        }
    }
}
