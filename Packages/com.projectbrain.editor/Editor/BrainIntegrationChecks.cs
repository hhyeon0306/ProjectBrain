using System;
using System.IO;
using System.Linq;

namespace ProjectBrain
{
    // Failure/retry and cross-entry-point contracts. All writes stay in isolated fixtures.
    public static class BrainIntegrationChecks
    {
        public static int RunInEditor()
        {
            int passed = 0;
            Action<bool, string> check = (ok, message) => { if (!ok) throw new Exception(message); passed++; };
            Action<Action> reject = action => { BrainStoreChecks.Reject(action); passed++; };
            var root = Path.Combine(Path.GetTempPath(), "BrainIntegration-" + Guid.NewGuid().ToString("N"));
            foreach (var folder in new[] { "Assets", "Packages", "ProjectSettings" }) Directory.CreateDirectory(Path.Combine(root, folder));
            File.WriteAllText(Path.Combine(root, "Assets/A.cs"), "// fixture");
            var json = new UnityBrainJson();
            var store = new BrainStore(Path.Combine(root, ".projectbrain"), json);
            var code = BrainStoreChecks.Node("asset:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "Code"); code.assetGuid = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            var feature = BrainStoreChecks.Node("feature:abilities/cast", "Feature");
            store.SaveNode(code); store.SaveNode(feature);
            var implementation = new BrainRelation { id = "relation:implementation", from = feature.id, to = code.id, type = "implemented_by", source = "fixture", createdUtc = DateTime.UtcNow.ToString("O") };
            store.SaveRelations(new[] { implementation });
            var tasks = new BrainTaskService(root, json);
            var task = tasks.Begin("Integration fixture", new[] { feature.id }, new[] { "Assets/A.cs" }).task;
            Func<string, string> resolve = _ => "Assets/A.cs";
            var verification = new BrainVerificationStore(root, json);
            var run = verification.Begin(task.id, "compile");
            var relationPath = Path.Combine(root, ".projectbrain/relations.json");
            bool interrupted = false;
            using (var locked = new FileStream(relationPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                try { verification.Finish(run.id, "passed", 1, 1, 0, 0, "synthetic fixture only"); }
                catch (IOException) { interrupted = true; }
            check(interrupted, "Locked graph write fails after terminal result is saved");
            var record = verification.Load(run.id);
            var recordPath = Path.Combine(root, ".projectbrain/evidence", run.id + ".json");
            var original = File.ReadAllBytes(recordPath);
            check(record.state == "passed" && !verification.IsPublished(record, new BrainGraphService(store), task.targetNodeIds), "Terminal outcome survives projection failure");
            check(new BrainCompletionService(root, json, resolve).Check().reasons.Any(r => r.code == "verification-publication"), "Missing projection is reported separately from execution");
            check(verification.Republish(run.id), "Repair appends missing projection");
            check(verification.IsPublished(record, new BrainGraphService(store), task.targetNodeIds), "Repair completes original target link");
            using (var locked = new FileStream(relationPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                check(!verification.Republish(run.id), "Idempotent repair performs no redundant relation write");
            check(File.ReadAllBytes(recordPath).SequenceEqual(original), "Repair never rewrites execution outcome");
            reject(() => verification.Finish(run.id, "failed", 1, 0, 1, 0, "overwrite"));
            check(store.LoadRelations().Count(r => r.to == "evidence:" + run.id) == 1, "No duplicate evidence edge");
            var evidencePath = Path.Combine(root, ".projectbrain/nodes", BrainStore.SafeId("evidence:" + run.id) + ".json");
            var evidenceText = File.ReadAllText(evidencePath);
            File.WriteAllText(evidencePath, evidenceText.Replace("synthetic fixture only", "conflicting text"));
            reject(() => verification.Republish(run.id));
            check(File.ReadAllText(evidencePath).Contains("conflicting text"), "Conflicting immutable projection preserved");
            File.WriteAllText(evidencePath, evidenceText);

            var sync = new BrainDocumentSync(Path.Combine(root, ".projectbrain"), json, resolve);
            var source = new ScriptDocument { scriptGuid = code.assetGuid, role = "Old role", designIntent = "Intent", cautions = "Caution", body = "Old body", lastKnownPath = "Assets/A.cs", savedCodeHash = new BrainWorkspace(root).FileHash("Assets/A.cs"), updatedUtc = DateTime.UtcNow.ToString("O") };
            sync.Save(source, sync.Version(source.scriptGuid));
            var edits = new BrainEditService(root, json, resolve);
            string docId = "document:" + source.scriptGuid;
            var view = edits.Read(task.id, task.revision, docId);
            check(view.documentFormat == "script-document" && view.structuredContent.Contains("Old body") && view.expectedDocumentVersion.Length == 64, "Agent can read structured source and version");
            var sourcePath = Path.Combine(root, ".projectbrain/docs", source.scriptGuid + ".json");
            var sourceBefore = File.ReadAllBytes(sourcePath);
            reject(() => edits.UpdateDocument(task.id, task.revision, docId, view.expectedHash, view.expectedContextHash, "Fork", "Fork"));
            check(File.ReadAllBytes(sourcePath).SequenceEqual(sourceBefore) && store.LoadNode(docId).body == BrainDocumentSync.Body(source), "Generic write cannot fork a structured document");
            var receipt = edits.UpdateScriptDocument(task.id, task.revision, docId, view.expectedHash, view.expectedContextHash, view.expectedDocumentVersion, "New role", "New intent", "New caution", "New body");
            var saved = new DocumentStore(Path.Combine(root, ".projectbrain/docs")).Load(source.scriptGuid);
            check(receipt.state == "applied" && saved.body == "New body" && saved.role == "New role", "Agent writes structured source");
            check(store.LoadNode(docId).summary == saved.role && store.LoadNode(docId).body == BrainDocumentSync.Body(saved), "Source and graph agree after agent write");
            check(receipt.afterHash == BrainWorkspace.Hash(json.Write(store.LoadNode(docId))), "Receipt identifies actual projected payload");
            reject(() => sync.Save(source, view.expectedDocumentVersion));
            reject(() => edits.UpdateScriptDocument(task.id, task.revision, docId, view.expectedHash, view.expectedContextHash, view.expectedDocumentVersion, "Lost", "", "", "Lost"));
            view = edits.Read(task.id, task.revision, docId);
            var beforeOversize = File.ReadAllBytes(sourcePath);
            reject(() => edits.UpdateScriptDocument(task.id, task.revision, docId, view.expectedHash, view.expectedContextHash, view.expectedDocumentVersion, new string('a', 70000), new string('b', 70000), "", ""));
            check(File.ReadAllBytes(sourcePath).SequenceEqual(beforeOversize), "Combined oversized projection is rejected before any source write");
            check(edits.UpdateScriptDocument(task.id, task.revision, docId, view.expectedHash, view.expectedContextHash, view.expectedDocumentVersion, saved.role, saved.designIntent, saved.cautions, saved.body).state == "unchanged", "Structured noop avoids another journal");
            var outside = new BrainEditService(root, json, _ => "Assets/B.cs"); File.WriteAllText(Path.Combine(root, "Assets/B.cs"), "outside");
            var outsideView = outside.Read(task.id, task.revision, docId);
            reject(() => outside.UpdateScriptDocument(task.id, task.revision, docId, outsideView.expectedHash, outsideView.expectedContextHash, outsideView.expectedDocumentVersion, "Denied", "", "", "Denied"));
            check(saved.body == new DocumentStore(Path.Combine(root, ".projectbrain/docs")).Load(source.scriptGuid).body, "Rejected writes preserve latest source");

            var baseline = new BrainContextService(new BrainGraphService(store), new BrainFreshnessService(root, json, resolve), json);
            check(json.Read<BrainContextResult>(baseline.Read(feature.id)).nodes.Any(n => n.id == docId), "Default context reaches a two-hop design document");
            var relations = store.LoadRelations().ToList();
            for (int i = 0; i < 100; i++)
            {
                var evidence = BrainStoreChecks.Node("evidence:" + Guid.NewGuid().ToString("D"), "Evidence"); evidence.updatedUtc = DateTime.UtcNow.AddSeconds(i).ToString("O");
                store.SaveNode(evidence);
                relations.Add(new BrainRelation { id = "relation:history" + i, from = feature.id, to = evidence.id, type = "verified_by", source = "fixture", createdUtc = evidence.updatedUtc });
            }
            store.SaveRelations(relations);
            var context = new BrainContextService(new BrainGraphService(store), new BrainFreshnessService(root, json, resolve), json);
            var text = context.Read(feature.id); var result = json.Read<BrainContextResult>(text);
            check(result.nodes.Any(n => n.id == code.id) && result.nodes.Any(n => n.id == docId), "100 history records cannot crowd out code/document");
            check(result.nodes.Count(n => n.type == "Evidence") <= 2 && result.omittedByHistory >= 99, "History cap is explicit");
            check(result.chars == text.Length + BrainWire.EnvelopeChars && result.chars <= 8000 && text == context.Read(feature.id), "Priority context remains bounded and deterministic");
            check(result.relations.All(r => result.nodes.Any(n => n.id == r.relation.from) && result.nodes.Any(n => n.id == r.relation.to)), "No dangling returned edges");
            return passed;
        }
    }
}
