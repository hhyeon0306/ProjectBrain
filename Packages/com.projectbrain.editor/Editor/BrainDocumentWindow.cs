using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectBrain
{
    public sealed class BrainDocumentWindow : EditorWindow
    {
        [SerializeField] private MonoScript selectedScript;
        [SerializeField] private ScriptDocument document;
        [SerializeField] private bool dirty;
        private readonly ScriptDocumentService service = new ScriptDocumentService();
        private VisualElement form;
        private VisualElement graph;
        private Label identity;
        private HelpBox message;

        [MenuItem("Window/Project Brain/Script Document")]
        public static void Open() => GetWindow<BrainDocumentWindow>("Brain 문서");

        public void CreateGUI()
        {
            minSize = new Vector2(820, 500);
            var root = rootVisualElement;
            root.Clear(); BrainTheme.Apply(root);
            root.style.paddingLeft = root.style.paddingRight = 12;
            root.style.paddingTop = root.style.paddingBottom = 12;
            var title = new Label("설계 문서 / 기존 스크립트 문서");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.fontSize = 16;
            title.style.marginBottom = 12;
            root.Add(title);
            var picker = new ObjectField("스크립트") { objectType = typeof(MonoScript), allowSceneObjects = false };
            picker.SetValueWithoutNotify(selectedScript);
            picker.RegisterValueChangedCallback(evt =>
            {
                if (!ConfirmDiscard()) { picker.SetValueWithoutNotify(selectedScript); return; }
                Run(() =>
                {
                    var nextScript = evt.newValue as MonoScript;
                    var next = nextScript == null ? null : service.LoadOrCreate(nextScript);
                    selectedScript = nextScript;
                    document = next;
                    SetDirty(false);
                    BuildForm();
                });
                picker.SetValueWithoutNotify(selectedScript);
            });
            root.Add(picker);
            var split = new TwoPaneSplitView(0, 270, TwoPaneSplitViewOrientation.Horizontal);
            split.style.flexGrow = 1;
            graph = new VisualElement();
            graph.style.minWidth = 230;
            split.Add(graph);
            form = new ScrollView();
            form.AddToClassList("document-page");
            form.style.minWidth = 330;
            form.style.flexGrow = 1;
            split.Add(form);
            root.Add(split);
            message = new HelpBox("C# 스크립트를 선택하세요. 기존 문서(docs) 편집이며 Explorer 문서(nodes)와 자동 동기화되지 않습니다.", HelpBoxMessageType.Info);
            root.Add(message);
            if (document == null && selectedScript != null) Run(() => document = service.LoadOrCreate(selectedScript));
            BuildForm();
            SetDirty(dirty);
        }

        private void BuildForm()
        {
            form.Clear();
            graph.Clear();
            if (document == null)
            {
                graph.Add(new Label("스크립트를 선택하면 관계 그래프가 표시됩니다."));
                return;
            }
            message.text = "기존 문서(docs) 편집 · Explorer 문서(nodes)와 자동 동기화되지 않습니다. 밝은 노드가 현재 문서입니다.";
            message.messageType = HelpBoxMessageType.Info;
            Run(() =>
            {
                var related = service.GetGraphRelatedGuids(document);
                graph.Add(new Label("직접 연결한 관련 코드 · 노드를 눌러 문서 열기"));
                graph.Add(new DocumentGraphView(document, related, script =>
                {
                    if (script == selectedScript || !ConfirmDiscard()) return;
                    Run(() =>
                    {
                        var next = service.LoadOrCreate(script);
                        selectedScript = script;
                        document = next;
                        SetDirty(false);
                        CreateGUI();
                    });
                }));
            });
            if (graph.childCount == 0)
            {
                graph.Add(new HelpBox("관계를 읽지 못했습니다. 아래 오류의 문서 파일을 확인한 뒤 다시 시도하세요. 현재 문서의 편집 내용은 유지됩니다.", HelpBoxMessageType.Error));
                graph.Add(new Button(BuildForm) { text = "관계 다시 읽기" });
            }
            identity = new Label("GUID: " + document.scriptGuid + "\n" + service.ResolvePath(document));
            identity.style.whiteSpace = WhiteSpace.Normal;
            identity.style.marginTop = identity.style.marginBottom = 12;
            form.Add(identity);
            AddText("역할", document.role, value => document.role = value);
            AddText("설계 의도", document.designIntent, value => document.designIntent = value);
            AddText("주의사항", document.cautions, value => document.cautions = value);
            AddText("본문", document.body, value => document.body = value);
            BuildImages();
            BuildRelations();
            form.Add(new HelpBox("문서 저장은 코드 검증이나 검토 승인을 의미하지 않습니다.", HelpBoxMessageType.Info));
            var save = new Button(() => SaveChanges()) { text = "문서 저장" }; save.AddToClassList("primary"); form.Add(save);
            form.Add(new Button(() => Run(() =>
            {
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(service.ResolvePath(document));
                if (script == null) throw new InvalidOperationException("연결된 코드가 없습니다.");
                AssetDatabase.OpenAsset(script);
            })) { text = "연결된 코드 열기" });
            form.Add(new Button(() =>
            {
                if (!ConfirmDiscard()) return;
                Run(() => { document = service.LoadOrCreate(selectedScript); SetDirty(false); BuildForm(); });
            }) { text = "저장된 문서 다시 읽기" });
        }

        private void AddText(string label, string value, Action<string> assign)
        {
            form.Add(new Label(label));
            var field = new TextField { multiline = true, value = value ?? "" };
            field.style.minHeight = 64;
            field.style.marginBottom = 10;
            field.RegisterValueChangedCallback(evt => { assign(evt.newValue); SetDirty(true); });
            form.Add(field);
        }

        private void BuildImages()
        {
            form.Add(new Label("첨부 이미지"));
            var picker = new ObjectField("이미지 추가") { objectType = typeof(Texture2D), allowSceneObjects = false };
            picker.RegisterValueChangedCallback(evt => Run(() =>
            {
                if (evt.newValue == null) return;
                var path = AssetDatabase.GetAssetPath(evt.newValue);
                if (!path.StartsWith("Assets/", StringComparison.Ordinal))
                    throw new InvalidOperationException("이미지를 Project 창의 Assets에 넣고 선택하세요.");
                var guid = AssetDatabase.AssetPathToGUID(path);
                document.imageGuids = (document.imageGuids ?? Array.Empty<string>()).Append(guid).Distinct().ToArray();
                SetDirty(true);
                BuildForm();
            }));
            form.Add(picker);
            foreach (var guid in document.imageGuids ?? Array.Empty<string>())
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture == null) form.Add(new HelpBox("이미지 누락: " + guid, HelpBoxMessageType.Warning));
                else
                {
                    var preview = new Image { image = texture, scaleMode = ScaleMode.ScaleToFit };
                    preview.style.height = 180;
                    form.Add(preview);
                    var label = new Label(path);
                    label.style.whiteSpace = WhiteSpace.Normal;
                    form.Add(label);
                }
                form.Add(new Button(() =>
                {
                    document.imageGuids = document.imageGuids.Where(id => id != guid).ToArray();
                    SetDirty(true);
                    BuildForm();
                }) { text = "이미지 연결 제거" });
            }
        }

        private void BuildRelations()
        {
            form.Add(new Label("관련 코드와 문서"));
            var picker = new ObjectField("관련 코드 추가") { objectType = typeof(MonoScript), allowSceneObjects = false };
            picker.RegisterValueChangedCallback(evt => Run(() =>
            {
                if (evt.newValue == null) return;
                var related = service.LoadOrCreate(evt.newValue as MonoScript);
                if (related.scriptGuid == document.scriptGuid)
                    throw new InvalidOperationException("다른 코드를 선택하세요.");
                document.relatedScriptGuids = (document.relatedScriptGuids ?? Array.Empty<string>()).Append(related.scriptGuid).Distinct().ToArray();
                SetDirty(true);
                BuildForm();
            }));
            form.Add(picker);
            foreach (var guid in document.relatedScriptGuids ?? Array.Empty<string>())
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                var label = new Label(script == null ? "코드 누락: " + guid : path);
                label.style.whiteSpace = WhiteSpace.Normal;
                form.Add(label);
                var open = new Button(() =>
                {
                    if (!ConfirmDiscard()) return;
                    Run(() =>
                    {
                        var next = service.LoadOrCreate(script);
                        selectedScript = script;
                        document = next;
                        SetDirty(false);
                        CreateGUI();
                    });
                }) { text = "관련 문서 열기 / 작성" };
                open.SetEnabled(script != null);
                form.Add(open);
                form.Add(new Button(() =>
                {
                    document.relatedScriptGuids = document.relatedScriptGuids.Where(id => id != guid).ToArray();
                    SetDirty(true);
                    BuildForm();
                }) { text = "관계 제거" });
            }
        }

        private bool ConfirmDiscard() => !dirty || EditorUtility.DisplayDialog("미저장 문서", "저장하지 않은 변경을 버릴까요?", "변경 버리기", "취소");
        private void SetDirty(bool value)
        {
            dirty = value;
            hasUnsavedChanges = value;
            saveChangesMessage = "Brain 문서의 변경을 저장할까요?";
        }
        public override void SaveChanges()
        {
            Run(() =>
            {
                if (document == null) return;
                service.Save(document);
                SetDirty(false);
                message.text = "저장했습니다: .projectbrain/docs/" + document.scriptGuid + ".json";
                message.messageType = HelpBoxMessageType.Info;
                identity.text = "GUID: " + document.scriptGuid + "\n" + service.ResolvePath(document);
            });
        }
        public override void DiscardChanges() { SetDirty(false); base.DiscardChanges(); }
        private void Run(Action action)
        {
            try { action(); }
            catch (Exception e) { if (message != null) { message.text = e.Message; message.messageType = HelpBoxMessageType.Error; } Debug.LogWarning("[Project Brain] " + e.Message); }
        }
    }
}
