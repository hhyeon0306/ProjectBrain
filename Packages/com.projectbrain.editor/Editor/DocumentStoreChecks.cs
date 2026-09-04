using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ProjectBrain
{
    public static class DocumentStoreChecks
    {
        // Batch entry point; isolated document storage never touches user documents.
        public static void RunBatch()
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
                service.Save(document);
                var restored = new DocumentStore(directory).Load(document.scriptGuid);
                Require(restored.role == document.role && restored.designIntent == document.designIntent && restored.cautions == document.cautions, "roundtrip"); passed++;
                Require(service.ResolvePath(restored) == "Assets/TutorialInfo/Scripts/Readme.cs" && restored.savedCodeHash.Length == 64, "GUID and hash"); passed++;
                restored.role = "갱신";
                service.Save(restored);
                Require(store.Load(restored.scriptGuid).role == "갱신", "atomic replacement"); passed++;
                ExpectFailure(() => store.Load("../escape")); passed++;
                var jsonPath = Path.Combine(directory, document.scriptGuid + ".json");
                File.WriteAllText(jsonPath, "not json");
                ExpectFailure(() => store.Load(document.scriptGuid)); passed++;
                ExpectFailure(() => store.Save(document));
                Require(File.ReadAllText(jsonPath) == "not json", "corrupt file preserved"); passed++;
                document.schemaVersion = 99;
                File.WriteAllText(jsonPath, JsonUtility.ToJson(document));
                ExpectFailure(() => store.Load(document.scriptGuid)); passed++;
                Require(Directory.GetFiles(directory, "*.tmp").Length == 0, "no temporary files"); passed++;
                Debug.Log("PROJECT_BRAIN_CHECKS_PASSED=" + passed);
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError("PROJECT_BRAIN_CHECKS_FAILED: " + e);
                EditorApplication.Exit(1);
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
