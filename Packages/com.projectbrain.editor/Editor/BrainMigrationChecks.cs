using System;
using System.IO;
using System.Linq;

namespace ProjectBrain
{
    public static class BrainMigrationChecks
    {
        public static int RunInEditor()
        {
            var codec = new UnityBrainJson();
            int passed = 0;
            Action<bool, string> check = (condition, label) => { if (!condition) throw new Exception(label); passed++; };
            var root = Fixture();
            const string guid = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            var doc = new ScriptDocument
            {
                scriptGuid = guid, lastKnownPath = "Assets/Old.cs", role = "역할", designIntent = "의도", cautions = "주의",
                body = "본문\n보존", savedCodeHash = "old-hash", updatedUtc = "2026-09-06T00:00:00Z",
                imageGuids = new[] { "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb" },
                relatedScriptGuids = new[] { "cccccccccccccccccccccccccccccccc", "cccccccccccccccccccccccccccccccc" }
            };
            var legacy = new DocumentStore(Path.Combine(root, "docs"));
            legacy.Save(doc);
            var sourcePath = Path.Combine(root, "docs", guid + ".json");
            var sourceBytes = File.ReadAllBytes(sourcePath);
            Func<string, string> resolve = id => id == guid ? "Assets/Renamed.cs" : "";
            var migration = new BrainMigration(root, codec, resolve);
            var result = migration.Run();
            var store = new BrainStore(root, codec);
            check(result.sources.Length == 1 && result.sources[0].originalVersion == 2 && result.sources[0].sha256.Length == 64, "Source version/hash receipt");
            check(result.nodeIds.Length == 4 && result.relationIds.Length == 3, "Code/document/image/related placeholder and deduplicated relations");
            check(File.ReadAllBytes(sourcePath).SequenceEqual(sourceBytes), "Original byte-for-byte preservation");
            var document = store.LoadNode("document:" + guid);
            check(document.body.Contains(doc.designIntent) && document.body.Contains(doc.cautions) && document.body.Contains(doc.body) && document.body.Contains(doc.savedCodeHash), "All legacy text/hash preserved");
            check(store.LoadNode("asset:" + guid).lastKnownPath == "Assets/Renamed.cs", "Resolve moved asset by GUID");
            check(store.LoadNodes().Count(n => n.status == "missing") == 2, "Missing related code/image explicitly represented");
            check(store.RelationsFor(document.id).Count == 2, "Document connects to code and image");
            var snapshot = Snapshot(root);
            migration.Run();
            check(Snapshot(root) == snapshot, "Repeated import writes no files and duplicates nothing");
            document.body = "User edit";
            store.SaveNode(document);
            migration.Run();
            check(store.LoadNode(document.id).body == "User edit", "Completed import preserves later user changes");
            doc.body = "Changed source";
            legacy.Save(doc);
            BrainStoreChecks.Reject(() => migration.Run()); passed++;
            check(store.LoadNode(document.id).body == "User edit", "Changed source never overwrites user data");

            var partial = Fixture();
            var partialDocs = new DocumentStore(Path.Combine(partial, "docs"));
            partialDocs.Save(doc);
            // Simulate interruption after nodes/relations but before the receipt commit.
            var partialMigration = new BrainMigration(partial, codec, resolve);
            partialMigration.Run();
            File.Delete(Path.Combine(partial, "migration.json"));
            partialMigration.Run();
            check(new BrainStore(partial, codec).LoadNodes().Count == 4, "Resume exact partial import");

            var conflict = Fixture();
            new DocumentStore(Path.Combine(conflict, "docs")).Save(doc);
            var conflictStore = new BrainStore(conflict, codec);
            var existing = BrainStoreChecks.Node("document:" + guid, "Document");
            existing.body = "Existing user content";
            conflictStore.SaveNode(existing);
            var before = Snapshot(conflict);
            BrainStoreChecks.Reject(() => new BrainMigration(conflict, codec, resolve).Run()); passed++;
            check(Snapshot(conflict) == before, "Conflicting import has no partial writes");

            var corrupt = Fixture();
            Directory.CreateDirectory(Path.Combine(corrupt, "docs"));
            File.WriteAllText(Path.Combine(corrupt, "docs", guid + ".json"), "broken json");
            BrainStoreChecks.Reject(() => new BrainMigration(corrupt, codec, resolve).Run()); passed++;
            check(!Directory.Exists(Path.Combine(corrupt, "nodes")) && !File.Exists(Path.Combine(corrupt, "migration.json")), "Bad source cannot create completion receipt");
            File.WriteAllText(Path.Combine(partial, "migration.json"), "broken receipt");
            BrainStoreChecks.Reject(() => partialMigration.Run()); passed++;
            check(File.ReadAllText(Path.Combine(partial, "migration.json")) == "broken receipt", "Corrupt receipt preserved");
            return passed;
        }
        private static string Fixture() => Path.Combine(Path.GetTempPath(), "BrainMigrationChecks-" + Guid.NewGuid().ToString("N"));
        private static string Snapshot(string root) => string.Join("\n", Directory.GetFiles(root, "*.json", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.Ordinal).Select(p => p + "\n" + File.ReadAllText(p)));
    }
}
