using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectBrain
{
    public static class BrainPresentationChecks
    {
        public static int RunInEditor()
        {
            int checks = 0;
            Action<bool, string> assert = (ok, label) => { if (!ok) throw new Exception(label); checks++; };
            var ids = Enumerable.Range(0, 200).Select(_ => Guid.NewGuid().ToString("D")).ToArray();
            var first = BrainRecordBrowser.AppendNumbers(new BrainRecordBrowser.NumberIndex(), ids);
            var reordered = BrainRecordBrowser.AppendNumbers(first, ids.Reverse());
            assert(first.entries.Length == 200 && first.entries.Select(e => e.number).Distinct().Count() == 200, "Unique display numbers for 200 task records");
            assert(first.entries.All(e => reordered.entries.Single(r => r.id == e.id).number == e.number), "Sorting cannot renumber a task");
            var appended = BrainRecordBrowser.AppendNumbers(first, new[] { Guid.NewGuid().ToString("D") }.Concat(ids));
            assert(appended.entries.Last().number == 201 && appended.entries[0].number == 1, "Late discovery appends and preserves existing numbers");
            bool rejected = false;
            try { BrainRecordBrowser.AppendNumbers(new BrainRecordBrowser.NumberIndex { entries = new[] { first.entries[0], first.entries[0] } }, ids); }
            catch (System.IO.InvalidDataException) { rejected = true; }
            assert(rejected, "Duplicate number registry is rejected");
            var graph = BrainPresentation.Graph();
            var root = graph.Nodes.Values.First(n => n.type == "Project");
            var contents = BrainPresentation.Contents(graph, root.id);
            assert(graph.Children(root.id).All(contents.Contains), "Root resolves directly contained domains");
            var domain = graph.Nodes.Values.First(n => n.type == "Domain" && BrainPresentation.Contents(graph, n.id).Any(x => graph.Get(x).type == "Code"));
            assert(BrainPresentation.Contents(graph, domain.id).Any(x => graph.Get(x).type == "Code"), "Domain includes code under child features");
            var code = graph.Nodes.Values.First(n => n.type == "Code" && graph.Relations.Any(r => r.from == n.id && r.type == "documented_by"));
            var documentId = graph.Relations.First(r => r.from == code.id && r.type == "documented_by").to;
            assert(BrainPresentation.CodeFor(graph, graph.Get(documentId))?.id == code.id, "Design and code resolve the same underlying document");
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            var window = ScriptableObject.CreateInstance<BrainDocumentWindow>();
            try
            {
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(code.assetGuid));
                var draft = new ScriptDocumentService().LoadOrCreate(script); draft.body = "U1-10 draft preservation fixture";
                Action<string, object> set = (name, value) => typeof(BrainDocumentWindow).GetField(name, flags).SetValue(window, value);
                set("selectedScript", script); set("document", draft); set("dirty", true); set("editing", true); window.CreateGUI();
                set("editing", false); set("readingId", root.id); window.CreateGUI();
                assert(window.hasUnsavedChanges && draft.body == "U1-10 draft preservation fixture", "Reading another page preserves an unsaved editor draft");
                set("editing", true); window.CreateGUI();
                assert(window.rootVisualElement.Q<TextField>("document-body").value == draft.body, "Returning to editor restores the same draft");
                set("editing", false); set("readingId", "asset:missing-fixture"); window.CreateGUI();
                assert(window.rootVisualElement.Query<Label>().ToList().Any(l => l.text.Contains("현재 구조도에 없습니다")), "Missing target renders explicit recovery state");
            }
            finally { window.DiscardChanges(); UnityEngine.Object.DestroyImmediate(window); }
            return checks;
        }
    }
}
