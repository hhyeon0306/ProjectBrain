using System;
using System.IO;
using System.Linq;

namespace ProjectBrain
{
    public static class BrainWorkflowChecks
    {
        public static int RunInEditor()
        {
            int passed = 0;
            Action<bool, string> check = (ok, label) => { if (!ok) throw new Exception(label); passed++; };
            Action<Action> reject = action => { BrainStoreChecks.Reject(action); passed++; };
            var root = Path.Combine(Path.GetTempPath(), "BrainWorkflowChecks-" + Guid.NewGuid().ToString("N"));
            foreach (var directory in new[] { "Assets", "Packages", "ProjectSettings", "Docs" }) Directory.CreateDirectory(Path.Combine(root, directory));
            File.WriteAllText(Path.Combine(root, "Packages/manifest.json"), "{\"dependencies\":{}}");
            File.WriteAllText(Path.Combine(root, "Assets/A.cs"), "before");
            File.WriteAllText(Path.Combine(root, "Assets/A.cs.meta"), "guid: a");
            File.WriteAllText(Path.Combine(root, "Assets/Deleted.cs"), "before");
            var json = new UnityBrainJson(); var store = new BrainStore(Path.Combine(root, ".projectbrain"), json);
            var domain = BrainStoreChecks.Node("domain:player", "Domain");
            var feature = BrainStoreChecks.Node("feature:player/movement", "Feature");
            var a = BrainStoreChecks.Node("asset:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "Code"); a.assetGuid = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"; a.lastKnownPath = "Assets/A.cs";
            var b = BrainStoreChecks.Node("document:a", "Document"); b.summary = new string('한', 600); b.body = new string('x', 30000);
            foreach (var node in new[] { domain, feature, a, b }) store.SaveNode(node);
            var edges = new[] { Edge("contains", domain.id, feature.id, "contains"), Edge("code", feature.id, a.id, "implemented_by"), Edge("doc", a.id, b.id, "documented_by"), Edge("cycle-a", a.id, b.id, "depends_on"), Edge("cycle-b", b.id, a.id, "depends_on") };
            store.SaveRelations(edges);
            Func<BrainGraphService> graph = () => new BrainGraphService(store);
            check(graph().Parent(feature.id) == domain.id && graph().Children(domain.id).Single() == feature.id, "Hierarchy direction");
            edges[0].from = feature.id; edges[0].to = domain.id; store.SaveRelations(edges);
            reject(() => graph());
            edges[0].from = domain.id; edges[0].to = feature.id; store.SaveRelations(edges);
            var otherDomain = BrainStoreChecks.Node("domain:other", "Domain"); store.SaveNode(otherDomain);
            store.SaveRelations(edges.Concat(new[] { Edge("other-parent", otherDomain.id, feature.id, "contains") }));
            reject(() => graph()); store.SaveRelations(edges);
            var tasks = new BrainTaskService(root, json);
            reject(() => tasks.Begin("bad", new[] { feature.id }, new[] { "Assets/../outside" }));
            check(tasks.Load() == null, "Invalid begin did not create task");
            var first = tasks.Begin("movement task", new[] { feature.id }, new[] { "Assets/A.cs" });
            check(!first.resumed && first.changes.Length == 0 && first.task.baseline.files.Length == 4, "Baseline includes existing files and meta");
            var taskPath = Path.Combine(root, ".projectbrain/tasks/active.json"); var initial = File.ReadAllText(taskPath);
            var resumed = new BrainTaskService(root, json).Begin("different purpose", new[] { domain.id }, new[] { "Assets/" });
            check(resumed.resumed && resumed.task.id == first.task.id && resumed.task.purpose == first.task.purpose && File.ReadAllText(taskPath) == initial, "New service resumes without overwriting task");
            reject(() => tasks.Begin("", null, null, Guid.NewGuid().ToString("D")));
            check(File.ReadAllText(taskPath) == initial, "Conflicting begin preserves bytes");
            var updated = tasks.Update(first.task.id, 1, "progress", "decision and reason", "open issue", "next", new[] { b.id });
            check(updated.revision == 2 && new BrainTaskService(root, json).Load().decisions == "decision and reason", "Summary survives new service");
            check(json.Write(updated.baseline) == json.Write(first.task.baseline), "Summary does not refresh baseline");
            var saved = File.ReadAllText(taskPath);
            reject(() => tasks.Update(first.task.id, 1, "lost", "", "", "", Array.Empty<string>()));
            reject(() => tasks.Update(first.task.id, 2, "lost", "", "", "", new[] { "document:missing" }));
            check(File.ReadAllText(taskPath) == saved, "Revision/reference conflicts preserve bytes");
            File.WriteAllText(Path.Combine(root, "Assets/A.cs"), "after");
            File.Delete(Path.Combine(root, "Assets/Deleted.cs"));
            File.WriteAllText(Path.Combine(root, "Assets/Renamed.cs"), "before");
            File.WriteAllText(Path.Combine(root, "Assets/A.cs.meta"), "guid: changed");
            File.WriteAllText(Path.Combine(root, "ProjectSettings/new.asset"), "setting");
            File.WriteAllText(Path.Combine(root, "Docs/ignored.md"), "ignored");
            var status = new BrainTaskService(root, json).Status();
            check(status.changes.Length == 5 && status.changes.Single(c => c.path == "Assets/A.cs").allowed, "All watched changes detected");
            check(status.changes.Count(c => !c.allowed) == 4 && status.changes.Any(c => c.kind == "deleted") && status.changes.Any(c => c.kind == "added"), "Outside allowed/meta/rename changes retained");
            File.WriteAllText(Path.Combine(root, "Packages/manifest.json"), "{\"dependencies\":{\"external\":\"file:../outside\"}}");
            check(tasks.Status().coverageLimitations.Any(s => s.Contains("file-package")), "External package coverage reported");
            File.WriteAllText(taskPath, saved.Replace("\"revision\": 2,", "")); reject(() => tasks.Load());
            reject(() => tasks.Begin("", null, null)); check(File.ReadAllText(taskPath).Contains("\"revision\"") == false, "Corrupt active task preserved");
            File.WriteAllText(taskPath, saved);

            Func<string, string> resolve = guid => guid == a.assetGuid ? "Assets/A.cs" : "";
            var freshness = new BrainFreshnessService(root, json, resolve);
            check(freshness.Inspect(graph(), b.id).state == "unknown", "No basis is unknown");
            freshness.Record(graph(), b.id, new[] { a.id }); freshness.Record(graph(), edges[2].id, new[] { a.id });
            check(new BrainFreshnessService(root, json, resolve).Inspect(graph(), b.id).state == "current", "Basis survives service restart");
            File.WriteAllText(Path.Combine(root, "Assets/A.cs"), "changed again");
            check(freshness.Inspect(graph(), b.id).state == "stale" && freshness.Inspect(graph(), edges[2].id).state == "stale", "Code change stales document and relation");
            File.WriteAllText(Path.Combine(root, "Assets/A.cs"), "after");
            check(freshness.Inspect(graph(), b.id).state == "current", "Exact code restoration matches basis");
            b.body += "edit"; store.SaveNode(b);
            check(freshness.Inspect(graph(), b.id).state == "stale", "Document edit stales basis");
            edges[2].source = "changed-source"; store.SaveRelations(edges);
            check(freshness.Inspect(graph(), edges[2].id).state == "stale", "Relation edit stales basis");
            File.Delete(Path.Combine(root, "Assets/A.cs"));
            check(freshness.Inspect(graph(), b.id).state == "missing", "Deleted code is missing");
            File.WriteAllText(Path.Combine(root, "Assets/A.cs"), "after");

            var reference = BrainStoreChecks.Node("reference:late", "Reference");
            var extra = BrainStoreChecks.Node("document:early", "Document");
            store.SaveNode(reference); store.SaveNode(extra);
            store.SaveRelations(edges.Concat(new[] { Edge("late", feature.id, reference.id, "references"), Edge("early", b.id, extra.id, "depends_on") }));
            var context = new BrainContextService(graph(), freshness, json);
            var layerIds = json.Read<BrainContextResult>(context.Read(a.id, 2, 12, 20000)).nodes.Select(n => n.id).ToList();
            check(layerIds.IndexOf(extra.id) < layerIds.IndexOf(reference.id), "Relation priority is global across the BFS layer");
            var text = context.Read(a.id, 2, 12, 8000); var parsed = json.Read<BrainContextResult>(text);
            check(text.Length + BrainWire.EnvelopeChars == parsed.chars && parsed.chars <= 8000 && parsed.nodes.Select(n => n.id).Distinct().Count() == parsed.nodes.Length, "Bounded cycle-safe context");
            check(text == context.Read(a.id, 2, 12, 8000), "Deterministic ordering");
            check(!text.Contains(new string('x', 100)) && parsed.omittedByChars > 0, "Bodies excluded; summary clipping reported");
            check(parsed.relations.Any(e => e.relation.from == a.id && e.relation.to == b.id && e.relation.source == "changed-source"), "Relation direction/type/source retained");
            var small = context.Read(domain.id, 0, 1, 1000); var smallParsed = json.Read<BrainContextResult>(small);
            check(small.Length <= 1000 && smallParsed.omittedByDepth > 0, "Depth and minimal budget");
            var capped = json.Read<BrainContextResult>(context.Read(a.id, 2, 1, 1800));
            check(capped.nodes.Length == 1 && capped.omittedByNodes > 0 && capped.nextNodeIds.Length > 0, "Node omissions and followup IDs");
            var charCapped = json.Read<BrainContextResult>(context.Read(a.id, 2, 12, 1800));
            check(charCapped.chars <= 1800 && charCapped.omittedByChars > 0, "Character budget includes metadata");
            reject(() => context.Read(a.id, 3)); reject(() => context.Read(a.id, 1, 31));
            var recordPath = Path.Combine(root, ".projectbrain/freshness/" + BrainStore.SafeId(b.id) + ".json");
            File.WriteAllText(recordPath, "{}"); reject(() => freshness.Record(graph(), b.id, new[] { a.id }));
            check(File.ReadAllText(recordPath) == "{}", "Corrupt freshness record preserved");
            return passed;
        }
        private static BrainRelation Edge(string id, string from, string to, string type) => new BrainRelation { id = "relation:" + id, from = from, to = to, type = type, source = "fixture", createdUtc = "2026-09-06T00:00:00Z" };
    }
}
