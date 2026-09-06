using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectBrain
{
    // Isolated regression fixtures for A1-R/F2; never writes production documents.
    public static class BrainRepairChecks
    {
        public static int RunInEditor()
        {
            int passed = 0;
            Action<bool, string> check = (ok, label) => { if (!ok) throw new Exception(label); passed++; };
            var root = Path.Combine(Path.GetTempPath(), "BrainRepairChecks-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var codec = new UnityBrainJson();
            var store = new BrainStore(root, codec);
            var relationsPath = Path.Combine(root, "relations.json");
            foreach (var invalid in new[] { "{}", "{\"schemaVersion\":1}", "{\"relations\":[]}", "{\"schemaVersion\":\"1\",\"relations\":[]}", "{\"schemaVersion\":1,\"relations\":null}", "{\"schemaVersion\":1,\"relations\":{}}", "{\"schemaVersion\":1,\"schemaVersion\":1,\"relations\":[]}", "{\"schemaVersion\":1,\"relations\":[null]}", "{\"schemaVersion\":1,\"relations\":[]} trailing" })
            {
                File.WriteAllText(relationsPath, invalid);
                BrainStoreChecks.Reject(() => store.LoadRelations()); passed++;
                BrainStoreChecks.Reject(() => store.SaveRelations(Array.Empty<BrainRelation>())); passed++;
                check(File.ReadAllText(relationsPath) == invalid, "Malformed relations preserved");
            }
            File.WriteAllText(relationsPath, "{\"schemaVersion\":1,\"relations\":[]}");
            check(store.LoadRelations().Count == 0, "Explicit empty relations accepted");
            store.SaveRelations(Array.Empty<BrainRelation>());
            var node = BrainStoreChecks.Node("domain:player", "Domain");
            store.SaveNode(node);
            var nodePath = Path.Combine(root, "nodes", BrainStore.SafeId(node.id) + ".json");
            var validNode = File.ReadAllText(nodePath);
            foreach (var field in new[] { "schemaVersion", "summary", "body", "status" })
            {
                var invalid = Regex.Replace(validNode, "(?m)^\\s*\"" + field + "\"[^\\r\\n]*,\\r?\\n", "");
                check(invalid != validNode, "Fixture removed " + field);
                File.WriteAllText(nodePath, invalid);
                BrainStoreChecks.Reject(() => store.LoadNode(node.id)); passed++;
                BrainStoreChecks.Reject(() => store.SaveNode(node)); passed++;
                check(File.ReadAllText(nodePath) == invalid, "Incomplete node preserved");
            }
            File.WriteAllText(nodePath, validNode);

            var docsPath = Path.Combine(root, "docs");
            var docs = new DocumentStore(docsPath);
            const string guid = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            var doc = new ScriptDocument { scriptGuid = guid };
            docs.Save(doc);
            var path = Path.Combine(docsPath, guid + ".json");
            foreach (var invalid in new[] { "{\"scriptGuid\":\"" + guid + "\"}", "{\"schemaVersion\":2,\"scriptGuid\":\"" + guid + "\",\"body\":12}", "{\"schemaVersion\":2,\"scriptGuid\":\"" + guid + "\",\"imageGuids\":[12]}" })
            {
                File.WriteAllText(path, invalid);
                BrainStoreChecks.Reject(() => docs.Load(guid)); passed++;
                BrainStoreChecks.Reject(() => docs.Save(doc)); passed++;
                check(File.ReadAllText(path) == invalid, "Incomplete document preserved");
            }
            File.WriteAllText(path, "{\"schemaVersion\":1,\"scriptGuid\":\"" + guid + "\",\"role\":\"v1\"}");
            check(docs.Load(guid).schemaVersion == 2 && docs.Load(guid).role == "v1", "Explicit v1 remains readable");
            BrainStoreChecks.Reject(() => codec.Read<BrainMigrationReceipt>("{\"completedUtc\":\"now\",\"sources\":[],\"nodeIds\":[],\"relationIds\":[]}")); passed++;

            var script = AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/TutorialInfo/Scripts/Readme.cs");
            var service = new ScriptDocumentService(docs);
            var selected = service.LoadOrCreate(script);
            selected.body = "unsaved text survives graph failure";
            File.WriteAllText(path, "{}");
            var window = ScriptableObject.CreateInstance<BrainDocumentWindow>();
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            Action<string, object> set = (name, value) => typeof(BrainDocumentWindow).GetField(name, flags).SetValue(window, value);
            try
            {
                set("service", service); set("document", selected); set("selectedScript", script); set("dirty", true);
                window.CreateGUI();
                var message = (HelpBox)typeof(BrainDocumentWindow).GetField("message", flags).GetValue(window);
                check(message.messageType == HelpBoxMessageType.Error && message.text.Contains(path), "UI reports corrupt file path without throwing");
                check(window.hasUnsavedChanges && selected.body == "unsaved text survives graph failure", "Unsaved state preserved");
                check(window.rootVisualElement.Query<TextField>().ToList().Any(f => f.value == selected.body), "Current document form remains editable");
                check(window.rootVisualElement.Query<Button>().ToList().Any(b => b.text == "관계 다시 읽기"), "Recovery action available");
                File.WriteAllText(path, codec.Write(doc));
                typeof(BrainDocumentWindow).GetMethod("BuildForm", flags).Invoke(window, null);
                check(message.messageType == HelpBoxMessageType.Info && window.hasUnsavedChanges, "Graph recovery preserves edits");
                check(!window.rootVisualElement.Query<Button>().ToList().Any(b => b.text == "관계 다시 읽기"), "Recovery replaces failure state");
            }
            finally { window.DiscardChanges(); UnityEngine.Object.DestroyImmediate(window); }
            return passed;
        }
    }
}
