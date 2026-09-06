using System;
using System.IO;
using System.Linq;

namespace ProjectBrain
{
    public static class BrainCompletionChecks
    {
        public static int RunInEditor()
        {
            var root = Path.Combine(Path.GetTempPath(), "BrainCompletionChecks-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(root, "Assets")); Directory.CreateDirectory(Path.Combine(root, "Packages")); Directory.CreateDirectory(Path.Combine(root, "ProjectSettings"));
            var codePath = Path.Combine(root, "Assets/A.cs"); File.WriteAllText(codePath, "before");
            var json = new UnityBrainJson(); var store = new BrainStore(Path.Combine(root, ".projectbrain"), json);
            var code = BrainStoreChecks.Node("asset:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "Code"); code.assetGuid = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"; code.lastKnownPath = "Assets/A.cs";
            var doc = BrainStoreChecks.Node("document:a", "Document"); doc.body = "Design";
            store.SaveNode(code); store.SaveNode(doc);
            var edge = new BrainRelation { id = "relation:review", from = code.id, to = doc.id, type = "documented_by", source = "test", createdUtc = DateTime.UtcNow.ToString("O") };
            store.SaveRelations(new[] { edge });
            var tasks = new BrainTaskService(root, json); var task = tasks.Begin("Review checks", new[] { code.id }, new[] { "Assets/A.cs" }).task;
            Func<string, string> resolve = guid => guid == code.assetGuid ? "Assets/A.cs" : "";
            var service = new BrainCompletionService(root, json, resolve);
            int passed = 0;
            Action<bool, string> check = (ok, label) => { if (!ok) throw new Exception(label); passed++; };
            Action<Action> reject = action => { BrainStoreChecks.Reject(action); passed++; };
            var taskPath = Path.Combine(root, ".projectbrain/tasks/active.json"); var originalTask = File.ReadAllText(taskPath);
            var status = service.Check(task.id, task.revision);
            check(!status.completed && status.reasons.Any(r => r.code.StartsWith("verification-")) && status.reasons.Any(r => r.code == "document-unreviewed"), "Initial refusal is explicit");
            new BrainFreshnessService(root, json, resolve).Record(new BrainGraphService(store), doc.id, new[] { code.id });
            check(service.InspectDocument(doc.id).state == "unreviewed", "AI basis does not approve");
            var review = service.InspectDocument(doc.id);
            reject(() => service.ConfirmFromHuman(task.id, 2, doc.id, review.snapshotHash));
            reject(() => service.ConfirmFromHuman(task.id, 1, doc.id, new string('0', 64)));
            service.ConfirmFromHuman(task.id, 1, doc.id, review.snapshotHash);
            check(new BrainCompletionService(root, json, resolve).InspectDocument(doc.id).state == "current", "Review survives restart");
            check(!service.Check().completed && service.Check().reasons.All(r => r.code.StartsWith("verification-")), "Even reviewed documents cannot substitute V1");
            // Synthetic runner outcomes test policy only; these are never production Unity evidence.
            var verification = new BrainVerificationStore(root, json);
            var zero = verification.Begin(task.id, "editmode");
            check(verification.Finish(zero.id, "passed", 0, 0, 0, 0, "fixture zero").state == "failed", "Zero tests rejected");
            var compile = verification.Begin(task.id, "compile");
            verification.Finish(compile.id, "passed", 1, 1, 0, 0, "fixture compile");
            check(!service.Check().ready, "Compile alone insufficient");
            var skipped = verification.Begin(task.id, "editmode");
            check(verification.Finish(skipped.id, "passed", 2, 1, 0, 1, "fixture skip").state == "failed", "Skipped tests rejected");
            var tests = verification.Begin(task.id, "editmode");
            verification.Finish(tests.id, "passed", 2, 2, 0, 0, "fixture tests");
            check(service.Check().ready, "Reviewed snapshot plus both passes eligible");
            var completed = service.Complete(task.id, 1);
            check(completed.completed && completed.activityId.StartsWith("activity:"), "Completion records activity");
            check(service.InspectDocument(doc.id).state == "current" && service.Check().ready, "Generated evidence/activity do not stale semantic review");
            reject(() => verification.Finish(tests.id, "failed", 1, 0, 1, 0, "overwrite"));
            File.WriteAllText(codePath, "after");
            check(service.InspectDocument(doc.id).state == "stale", "Code edit invalidates review");
            check(!service.Complete(task.id, 1).completed && service.Check().reasons.Count(r => r.code.StartsWith("verification-")) == 2, "Post-verification edits invalidate both passes");
            var changed = verification.Begin(task.id, "editmode");
            File.WriteAllText(codePath, "during run");
            check(verification.Finish(changed.id, "passed", 1, 1, 0, 0, "fixture changed").state == "stale", "Changes during execution invalidate success");
            var interrupted = verification.Begin(task.id, "editmode");
            check(verification.Finish(interrupted.id, "interrupted", 0, 0, 0, 0, "fixture reload").state == "interrupted", "Interrupted is not success");
            reject(() => service.ConfirmFromHuman(task.id, 1, doc.id, review.snapshotHash));
            File.WriteAllText(codePath, "before");
            var shown = json.Write(doc); doc.body = "Changed design"; store.SaveNode(doc);
            check(service.InspectDocument(doc.id).state == "stale", "Document edit invalidates review");
            reject(() => service.InspectDocument(doc.id, shown));
            doc.body = "Design"; store.SaveNode(doc); edge.source = "changed"; store.SaveRelations(new[] { edge });
            check(service.InspectDocument(doc.id).state == "stale", "Relation edit invalidates review");
            File.Delete(codePath);
            check(service.InspectDocument(doc.id).state == "missing-code", "Deleted code cannot be reviewed");
            reject(() => service.ConfirmFromHuman(task.id, 1, doc.id, service.InspectDocument(doc.id).snapshotHash));
            File.WriteAllText(codePath, "before");
            store.SaveRelations(Array.Empty<BrainRelation>());
            check(service.Check().reasons.Any(r => r.code == "missing-document"), "Removing links cannot bypass required documents");
            store.SaveRelations(new[] { edge });
            File.WriteAllText(Path.Combine(root, "ProjectSettings/new.asset"), "outside");
            check(service.Check().reasons.Any(r => r.code == "outside-allowed") && service.Check().reasons.Any(r => r.code == "unmapped-change"), "Unmapped out-of-scope changes remain blockers");
            File.WriteAllText(Path.Combine(root, "Packages/manifest.json"), "{\"dependencies\":{\"external\":\"file:../external\"}}");
            check(service.Check().reasons.Any(r => r.code == "coverage-limited"), "Coverage blocks completion");
            reject(() => service.Check(Guid.NewGuid().ToString("D"), 1));
            reject(() => service.Check(task.id, 2));
            check(File.ReadAllText(taskPath) == originalTask, "Refusal/review preserve active revision and baseline");
            var reviewPath = Path.Combine(root, ".projectbrain/reviews", BrainWorkspace.Hash(task.id + "\n" + doc.id) + ".json");
            File.WriteAllText(reviewPath, "{}");
            reject(() => service.Check());
            reject(() => service.ConfirmFromHuman(task.id, 1, doc.id, review.snapshotHash));
            check(File.ReadAllText(reviewPath) == "{}", "Corrupt review protected");
            return passed;
        }
    }
}
