using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace ProjectBrain
{
    public static class BrainDocumentSyncChecks
    {
        public static int RunInEditor()
        {
            var project = Path.Combine(Path.GetTempPath(), "BrainDocumentSyncChecks-" + Guid.NewGuid().ToString("N"));
            var root = Path.Combine(project, ".projectbrain");
            var json = new UnityBrainJson(); int passed = 0;
            const string a = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", b = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb", c = "cccccccccccccccccccccccccccccccc";
            Func<string, string> resolve = guid => guid == a ? "Assets/A.cs" : guid == b ? "Assets/B.cs" : guid == c ? "Assets/C.png" : "";
            Action<bool, string> check = (ok, why) => { if (!ok) throw new Exception(why); passed++; };
            Action<Action> reject = action => { bool rejected = false; try { action(); } catch (Exception) { rejected = true; } check(rejected, "Expected rejection"); };
            Func<Dictionary<string, string>> bytes = () => Directory.GetFiles(root, "*.json", SearchOption.AllDirectories).ToDictionary(p => p, p => Convert.ToBase64String(File.ReadAllBytes(p)));
            try
            {
                Directory.CreateDirectory(Path.Combine(project, "Assets"));
                File.WriteAllText(Path.Combine(project, "Assets/A.cs"), "fixture");
                var sync = new BrainDocumentSync(root, json, resolve);
                var d = new ScriptDocument { scriptGuid = a, lastKnownPath = "Assets/A.cs", role = "역할", designIntent = "설계\n개행", cautions = "주의", body = "본문", savedCodeHash = "old", updatedUtc = "2026-09-07T00:00:00Z", imageGuids = new[] { c }, relatedScriptGuids = new[] { b } };
                sync.Save(d, sync.Version(a));
                var store = new BrainStore(root, json); var docs = new DocumentStore(Path.Combine(root, "docs"));
                check(store.LoadNode("document:" + a).body == BrainDocumentSync.Body(d), "All structured text projects losslessly");
                check(docs.Load(a).designIntent == d.designIntent, "Source Korean/newlines saved");
                check(store.LoadRelations().Count == 3 && store.LoadNodes().Count == 4, "New document, image, linked code and edges created");
                var saved = bytes(); sync.Save(d, sync.Version(a));
                check(saved.Count == bytes().Count && saved.All(p => bytes()[p.Key] == p.Value), "No-op save preserves every byte and history count");

                var target = store.LoadNode("document:" + a); target.title = "사용자 제목"; target.tags = new[] { "manual-tag" }; store.SaveNode(target);
                var manual = new BrainRelation { id = "relation:incoming", from = "asset:" + b, to = "asset:" + a, type = "depends_on", source = "manual", createdUtc = d.updatedUtc };
                var all = store.LoadRelations().ToList(); all.Add(manual); store.SaveRelations(all);
                d.role = "새 역할"; d.body = "새 본문"; d.imageGuids = Array.Empty<string>(); d.relatedScriptGuids = Array.Empty<string>();
                sync.Save(d, sync.Version(a));
                check(store.LoadNode(target.id).summary == d.role && store.LoadNode(target.id).body == BrainDocumentSync.Body(d), "Edited text automatically projects");
                check(store.LoadNode(target.id).title == target.title && store.LoadNode(target.id).tags.Single() == "manual-tag", "Existing title/tags preserved");
                check(store.LoadRelations().Count == 2 && store.LoadRelations().Any(r => r.id == manual.id), "Only owned outgoing image/code links removed; incoming manual edge preserved");
                check(store.LoadNode("asset:" + b) != null && store.LoadNode("asset:" + c) != null, "Unlink does not delete shared nodes");

                var version = sync.Version(a); target = store.LoadNode(target.id); target.body = "Concurrent agent edit"; store.SaveNode(target); var concurrent = bytes();
                reject(() => sync.Save(d, version));
                check(concurrent.All(p => bytes()[p.Key] == p.Value), "Concurrent node edit rejected without any writes");
                version = sync.Version(a); d.relatedScriptGuids = new[] { a }; var self = bytes();
                reject(() => sync.Save(d, version));
                check(self.All(p => bytes()[p.Key] == p.Value), "Invalid self-link preflight preserves all files");
                d.relatedScriptGuids = Array.Empty<string>();

                var path = Path.Combine(root, "nodes", BrainStore.SafeId(target.id) + ".json"); var original = File.ReadAllBytes(path);
                File.WriteAllText(path, "{broken");
                reject(() => sync.Save(d, version)); check(File.ReadAllText(path) == "{broken", "Corrupt destination is never overwritten");
                File.WriteAllBytes(path, original);

                var beforeCrash = bytes(); d.body = "중단 후 복구 내용";
                var failing = new BrainDocumentSync(root, json, resolve, count => { if (count == 1) throw new IOException("fixture interruption"); });
                reject(() => failing.Save(d, failing.Version(a)));
                check(File.Exists(Path.Combine(root, "document-sync/pending.json")), "Durable intent survives interruption");
                store = new BrainStore(root, json); docs = new DocumentStore(Path.Combine(root, "docs"));
                check(docs.Load(a).body == d.body && store.LoadNode(target.id).body == BrainDocumentSync.Body(d), "Reopening store resumes all files consistently");
                check(!File.Exists(Path.Combine(root, "document-sync/pending.json")), "Successful recovery clears pending intent");
                var histories = Directory.GetFiles(Path.Combine(root, "document-sync/history"), "*.json").Select(p => json.Read<BrainDocumentSyncBatch>(File.ReadAllText(p))).ToArray();
                check(histories.Any(h => h.writes.Any(w => w.path == "docs/" + a + ".json" && w.before == beforeCrash[Path.Combine(root, "docs", a + ".json")])), "Exact old source bytes retained in history");

                var tasks = new BrainTaskService(project, json); var task = tasks.Begin("isolated sync review", new[] { "asset:" + a }, new[] { "Assets/A.cs" }).task;
                var reviews = new BrainCompletionService(project, json, resolve);
                var review = reviews.InspectDocument(target.id);
                reviews.ConfirmFromHuman(task.id, task.revision, target.id, review.snapshotHash); // Synthetic UI assertion only in this temporary fixture.
                check(reviews.InspectDocument(target.id).state == "current", "Fixture review established");
                d.cautions = "변경된 주의사항"; sync.Save(d, sync.Version(a));
                check(reviews.InspectDocument(target.id).state == "stale", "Document sync invalidates prior human review; never approves");

                version = sync.Version(a); d.body = "conflicting recovery";
                reject(() => failing.Save(d, version));
                File.WriteAllText(Path.Combine(root, "docs", a + ".json"), "external source edit");
                var beforeConflict = bytes();
                reject(() => new BrainStore(root, json));
                check(beforeConflict.All(p => bytes()[p.Key] == p.Value), "Recovery conflict preserves every original/pending byte");
                var pendingPath = Path.Combine(root, "document-sync/pending.json");
                var pending = json.Read<BrainDocumentSyncBatch>(File.ReadAllText(pendingPath)); pending.writes[0].path = "../outside.json";
                File.WriteAllText(pendingPath, json.Write(pending)); reject(() => new BrainStore(root, json));
                check(!File.Exists(Path.Combine(project, "outside.json")), "Recovery refuses paths outside fixed document targets");
                return passed;
            }
            finally { if (Directory.Exists(project)) Directory.Delete(project, true); }
        }
    }
}
