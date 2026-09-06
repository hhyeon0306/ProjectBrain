using System;
using System.IO;
using System.Linq;
using System.Text;

namespace ProjectBrain
{
    public static class BrainEditChecks
    {
        public static int RunInEditor()
        {
            var root = Path.Combine(Path.GetTempPath(), "BrainEdit-" + Guid.NewGuid().ToString("N"));
            foreach (var dir in new[] { "Assets", "Packages", "ProjectSettings" }) Directory.CreateDirectory(Path.Combine(root, dir));
            var path = Path.Combine(root, "Assets/A.cs"); File.WriteAllText(path, "// before\r\n", new UTF8Encoding(true));
            var json = new UnityBrainJson(); var store = new BrainStore(Path.Combine(root, ".projectbrain"), json);
            var code = BrainStoreChecks.Node("asset:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "Code"); code.assetGuid = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            var doc = BrainStoreChecks.Node("document:a", "Document"); doc.body = "old design";
            store.SaveNode(code); store.SaveNode(doc);
            store.SaveRelations(new[] { new BrainRelation { id = "relation:doc", from = code.id, to = doc.id, type = "documented_by", source = "fixture", createdUtc = DateTime.UtcNow.ToString("O") } });
            var task = new BrainTaskService(root, json).Begin("Editing", new[] { code.id }, new[] { "Assets/A.cs" }).task;
            var taskPath = Path.Combine(root, ".projectbrain/tasks/active.json"); var originalTask = File.ReadAllText(taskPath);
            Func<string, string> resolve = guid => "Assets/A.cs";
            var edits = new BrainEditService(root, json, resolve); var reviews = new BrainCompletionService(root, json, resolve);
            int passed = 0; Action<bool, string> check = (ok, label) => { if (!ok) throw new Exception(label); passed++; };
            Action<Action> reject = action => { BrainStoreChecks.Reject(action); passed++; };
            var first = edits.Read(task.id, 1, code.id);
            check(first.content.StartsWith("\ufeff") && first.content.EndsWith("\r\n") && first.writable, "UTF8 BOM/newlines retained in edit read");
            var original = File.ReadAllBytes(path);
            reject(() => edits.Apply(task.id, 2, code.id, first.expectedHash, "lost"));
            reject(() => edits.Apply(task.id, 1, code.id, new string('0', 64), "lost"));
            check(File.ReadAllBytes(path).SequenceEqual(original) && !Directory.Exists(Path.Combine(root, ".projectbrain/edits")), "Conflicts have no writes/journal");
            var unchanged = edits.Apply(task.id, 1, code.id, first.expectedHash, first.content);
            check(unchanged.state == "unchanged" && !unchanged.needsAssetRefresh, "Noop avoids writes");
            var docFirst = edits.Read(task.id, 1, doc.id);
            var confirmed = reviews.InspectDocument(doc.id); reviews.ConfirmFromHuman(task.id, 1, doc.id, confirmed.snapshotHash);
            var receipt = edits.Apply(task.id, 1, code.id, first.expectedHash, first.content.Replace("before", "after"));
            check(receipt.state == "applied" && receipt.needsAssetRefresh && receipt.afterHash == BrainWorkspace.HashBytes(File.ReadAllBytes(path)), "Code applied and hashed");
            check(File.ReadAllBytes(path).Take(3).SequenceEqual(new byte[] { 239, 187, 191 }), "BOM survives write");
            check(json.Read<BrainEditReceipt>(File.ReadAllText(Path.Combine(root, receipt.recordPath))).state == "applied", "Receipt persisted");
            reject(() => edits.Apply(task.id, 1, code.id, first.expectedHash, "lost"));
            check(reviews.InspectDocument(doc.id).state == "stale", "Code edit invalidates human review");
            reject(() => edits.UpdateDocument(task.id, 1, doc.id, docFirst.expectedHash, docFirst.expectedContextHash, "new", "new"));
            var freshDoc = edits.Read(task.id, 1, doc.id);
            var updated = edits.UpdateDocument(task.id, 1, doc.id, freshDoc.expectedHash, freshDoc.expectedContextHash, "updated", "current design");
            check(updated.state == "applied" && !updated.needsAssetRefresh && store.LoadNode(doc.id).status == "unreviewed", "Document edit unreviewed");
            check(reviews.InspectDocument(doc.id).state == "stale", "AI write never replaces review record");
            reject(() => edits.UpdateDocument(task.id, 1, doc.id, freshDoc.expectedHash, freshDoc.expectedContextHash, "lost", "lost"));
            reject(() => edits.Apply(task.id, 1, code.id, receipt.afterHash, new string('x', 131073)));
            reject(() => edits.Apply(task.id, 1, code.id, receipt.afterHash, "a\0b"));
            var outside = new BrainEditService(root, json, guid => "Assets/B.cs"); File.WriteAllText(Path.Combine(root, "Assets/B.cs"), "outside");
            var outsideView = outside.Read(task.id, 1, code.id);
            check(!outsideView.writable, "Outside allowed readable but not writable");
            reject(() => outside.Apply(task.id, 1, code.id, outsideView.expectedHash, "lost"));
            var outsideDoc = outside.Read(task.id, 1, doc.id);
            reject(() => outside.UpdateDocument(task.id, 1, doc.id, outsideDoc.expectedHash, outsideDoc.expectedContextHash, "lost", "lost"));
            check(File.ReadAllText(taskPath) == originalTask, "Task revision/baseline preserved");
            var docPath = Path.Combine(root, ".projectbrain/nodes", BrainStore.SafeId(doc.id) + ".json"); File.WriteAllText(docPath, "{}");
            reject(() => edits.UpdateDocument(task.id, 1, doc.id, freshDoc.expectedHash, freshDoc.expectedContextHash, "lost", "lost"));
            check(File.ReadAllText(docPath) == "{}", "Corrupt node preserved");
            return passed;
        }
    }
}
