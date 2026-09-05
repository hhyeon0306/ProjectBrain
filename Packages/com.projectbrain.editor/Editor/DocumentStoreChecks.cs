using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ProjectBrain
{
    public static class DocumentStoreChecks
    {
        // Batch entry point; isolated document storage never touches user documents.
        public static void RunBatch()
        {
            try { RunInEditor(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogError(e); EditorApplication.Exit(1); }
        }

        // Safe for the open Editor: no scene changes or Editor exit.
        public static int RunInEditor()
        {
            var directory = Path.Combine(Path.GetTempPath(), "ProjectBrainChecks-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            int passed = 0;
            try
            {
                var store = new DocumentStore(directory);
                var service = new ScriptDocumentService(store);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/TutorialInfo/Scripts/Readme.cs");
                if (script == null) throw new Exception("Fixture script missing");
                var document = service.LoadOrCreate(script);
                Require(store.Load(document.scriptGuid) == null, "new document is not silently saved"); passed++;
                document.role = "한글 역할\n두 번째 줄";
                document.designIntent = "입력과 이동 분리";
                document.cautions = "검증과 저장 구분";
                document.body = "# 이동 설계\n```csharp\nMove(direction, dt);\n```";
                document.imageGuids = new[] { "0123456789abcdef0123456789abcdef" };
                document.relatedScriptGuids = new[] { "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" };
                service.Save(document);
                var restored = new DocumentStore(directory).Load(document.scriptGuid);
                Require(restored.role == document.role && restored.designIntent == document.designIntent && restored.cautions == document.cautions, "roundtrip"); passed++;
                Require(restored.body == document.body && restored.imageGuids[0] == document.imageGuids[0] && restored.relatedScriptGuids[0] == document.relatedScriptGuids[0], "body images relations roundtrip"); passed++;
                Require(service.ResolvePath(restored) == "Assets/TutorialInfo/Scripts/Readme.cs" && restored.savedCodeHash.Length == 64, "GUID and hash"); passed++;
                var inbound = new ScriptDocument
                {
                    scriptGuid = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
                    lastKnownPath = "Assets/Inbound.cs",
                    relatedScriptGuids = new[] { restored.scriptGuid }
                };
                store.Save(inbound);
                Require(service.GetGraphRelatedGuids(restored).Contains(inbound.scriptGuid), "incoming relation is navigable"); passed++;
                restored.role = "갱신";
                service.Save(restored);
                Require(store.Load(restored.scriptGuid).role == "갱신", "atomic replacement"); passed++;
                ExpectFailure(() => store.Load("../escape")); passed++;
                var jsonPath = Path.Combine(directory, document.scriptGuid + ".json");
                var legacy = "{\"schemaVersion\":1,\"scriptGuid\":\"" + document.scriptGuid + "\",\"role\":\"legacy\"}";
                File.WriteAllText(jsonPath, legacy);
                var migrated = store.Load(document.scriptGuid);
                Require(migrated.schemaVersion == 2 && migrated.body == "" && migrated.imageGuids.Length == 0 && migrated.role == "legacy", "v1 migration"); passed++;
                Require(File.ReadAllText(jsonPath) == legacy, "reading migration does not rewrite original"); passed++;
                service.Save(document);
                var validJson = File.ReadAllText(jsonPath);
                restored.imageGuids = new[] { "../escape" };
                ExpectFailure(() => store.Save(restored));
                Require(File.ReadAllText(jsonPath) == validJson, "invalid attachment preserves document"); passed++;
                File.WriteAllText(jsonPath, "not json");
                ExpectFailure(() => store.Load(document.scriptGuid)); passed++;
                ExpectFailure(() => store.Save(document));
                Require(File.ReadAllText(jsonPath) == "not json", "corrupt file preserved"); passed++;
                document.schemaVersion = 99;
                File.WriteAllText(jsonPath, JsonUtility.ToJson(document));
                ExpectFailure(() => store.Load(document.scriptGuid)); passed++;
                Require(Directory.GetFiles(directory, "*.tmp").Length == 0, "no temporary files"); passed++;
                Debug.Log("PROJECT_BRAIN_CHECKS_PASSED=" + passed);
                return passed;
            }
            catch (Exception e)
            {
                Debug.LogError("PROJECT_BRAIN_CHECKS_FAILED: " + e);
                throw;
            }
            // Fixture is retained in OS temp for inspection; no recursive deletion.
        }

        private static void Require(bool condition, string name)
        {
            if (!condition) throw new Exception("Check failed: " + name);
        }
        private static void ExpectFailure(Action action)
        {
            try { action(); }
            catch (InvalidDataException) { return; }
            throw new Exception("Expected invalid-data rejection");
        }
    }
}
