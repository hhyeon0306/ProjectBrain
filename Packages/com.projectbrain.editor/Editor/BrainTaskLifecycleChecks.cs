using System;
using System.IO;
using System.Linq;

namespace ProjectBrain
{
    public static class BrainTaskLifecycleChecks
    {
        public static int RunInEditor()
        {
            var root = Path.Combine(Path.GetTempPath(), "BrainLifecycle-" + Guid.NewGuid().ToString("N"));
            foreach (var dir in new[] { "Assets", "Packages", "ProjectSettings" }) Directory.CreateDirectory(Path.Combine(root, dir));
            File.WriteAllText(Path.Combine(root, "Assets/A.cs"), "before");
            var json = new UnityBrainJson(); var store = new BrainStore(Path.Combine(root, ".projectbrain"), json);
            var code = BrainStoreChecks.Node("asset:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "Code"); code.assetGuid = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            var doc = BrainStoreChecks.Node("document:a", "Document"); doc.body = "fixture design";
            store.SaveNode(code); store.SaveNode(doc);
            store.SaveRelations(new[] { new BrainRelation { id = "relation:doc", from = code.id, to = doc.id, type = "documented_by", source = "fixture", createdUtc = DateTime.UtcNow.ToString("O") } });
            var tasks = new BrainTaskService(root, json); var lifecycle = new BrainTaskLifecycle(root, json, _ => "Assets/A.cs");
            var task = tasks.Begin("fixture lifecycle", new[] { code.id }, new[] { "Assets/A.cs" }).task;
            var path = Path.Combine(root, ".projectbrain/tasks/active.json"); var original = File.ReadAllText(path);
            int passed = 0; Action<bool, string> check = (ok, label) => { if (!ok) throw new Exception(label); passed++; };
            Action<Action> reject = action => { BrainStoreChecks.Reject(action); passed++; };
            // Legacy active records omit only the newly introduced optional scope history.
            File.WriteAllText(path, original.Replace(",\n    \"scopeChanges\": []", ""));
            check(tasks.Load().scopeChanges.Length == 0, "Legacy scope history defaults empty");
            reject(() => lifecycle.SetScope(task.id, 2, new[] { "Assets/" }, "stale"));
            reject(() => lifecycle.SetScope(task.id, 1, new[] { "../bad" }, "invalid"));
            reject(() => lifecycle.SetScope(task.id, 1, new[] { "Assets/" }, ""));
            check(lifecycle.SetScope(task.id, 1, task.allowedPaths, "noop").revision == 1, "Noop scope does not increment");
            File.WriteAllText(Path.Combine(root, "Assets/B.cs"), "outside");
            check(tasks.Status().changes.Single().allowed == false, "Outside change visible");
            var scoped = lifecycle.SetScope(task.id, 1, new[] { "Assets/" }, "fixture expansion");
            check(scoped.revision == 2 && tasks.Status().changes.Single().allowed, "Explicit expansion reclassifies existing change");
            check(json.Write(tasks.Load().baseline) == json.Write(task.baseline), "Scope preserves original baseline");
            var history = lifecycle.History(task.id);
            check(history.scopeChanges.Length == 1 && history.scopeChanges[0].before.SequenceEqual(task.allowedPaths) && history.scopeChanges[0].reason == "fixture expansion", "Scope reason and before/after retained");
            reject(() => tasks.Update(task.id, 1, "", "", "", "", Array.Empty<string>()));
            var completion = new BrainCompletionService(root, json, _ => "Assets/A.cs");
            check(completion.Check().reasons.Any(r => r.code == "unmapped-change") && completion.Check().reasons.Any(r => r.code == "document-unreviewed"), "Scope does not approve unmapped changes or human review");
            reject(() => lifecycle.Close(task.id, 2, "completed", "not ready"));
            check(tasks.Load().id == task.id, "Rejected completion keeps active task");
            var verifications = new BrainVerificationStore(root, json); var running = verifications.Begin(task.id, "compile");
            reject(() => lifecycle.SetScope(task.id, 2, task.allowedPaths, "busy"));
            reject(() => lifecycle.Close(task.id, 2, "abandoned", "busy"));
            verifications.Finish(running.id, "interrupted", 0, 0, 0, 0, "fixture only");
            var archived = lifecycle.Close(task.id, 2, "abandoned", "fixture unfinished");
            check(tasks.Load() == null && archived.disposition == "abandoned" && archived.remainingReasons.Length > 0, "Abandon frees slot and preserves blockers");
            var archivePath = Path.Combine(root, archived.archivePath); var archiveBytes = File.ReadAllBytes(archivePath);
            check(lifecycle.Close(task.id, 2, "abandoned", "fixture unfinished").closedUtc == archived.closedUtc, "Same close retry idempotent");
            reject(() => lifecycle.Close(task.id, 2, "completed", "rewrite"));
            reject(() => tasks.Begin("", null, null, task.id));
            reject(() => tasks.Update(task.id, 2, "", "", "", "", Array.Empty<string>()));
            var next = tasks.Begin("next fixture", new[] { code.id }, new[] { "Assets/A.cs" }).task;
            check(next.id != task.id && next.baseline.files.Length == 2 && next.scopeChanges.Length == 0, "Next task gets new identity and current baseline");
            check(File.ReadAllBytes(archivePath).SequenceEqual(archiveBytes) && lifecycle.History(task.id).task.baselineFileCount == 1, "Previous archive stays immutable");
            check(lifecycle.Close(task.id, 2, "abandoned", "fixture unfinished").task.id == task.id && tasks.Load().id == next.id, "Old close retry cannot close new task");
            // Only this isolated fixture records a synthetic human assertion and test results.
            var review = completion.InspectDocument(doc.id); completion.ConfirmFromHuman(next.id, 1, doc.id, review.snapshotHash);
            foreach (var kind in new[] { "compile", "editmode" }) { var run = verifications.Begin(next.id, kind); verifications.Finish(run.id, "passed", 1, 1, 0, 0, "isolated fixture"); }
            var done = lifecycle.Close(next.id, 1, "completed", "fixture policy satisfied");
            check(done.disposition == "completed" && tasks.Load() == null && done.remainingReasons.Length == 0, "Completed closure requires policy and archives success");
            check(json.Read<BrainTaskArchive>(File.ReadAllText(Path.Combine(root, done.archivePath))).completion.activityId.StartsWith("activity:"), "Completed archive contains actual Activity receipt");
            File.WriteAllText(Path.Combine(root, done.archivePath), "{}");
            reject(() => tasks.Load());
            reject(() => tasks.Begin("bad overwrite", new[] { code.id }, new[] { "Assets/A.cs" }));
            check(File.ReadAllText(Path.Combine(root, done.archivePath)) == "{}", "Corrupt close marker preserved");
            return passed;
        }
    }
}
