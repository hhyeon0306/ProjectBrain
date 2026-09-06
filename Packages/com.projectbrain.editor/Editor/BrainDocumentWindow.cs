using System;
using System.IO;
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
        [SerializeField] private int activeTab;
        private readonly ScriptDocumentService service = new ScriptDocumentService();
        private ScrollView form;
        private VisualElement tabs;
        private Label documentTitle, subtitle, saveState, completeness;
        private HelpBox message;
        private Button saveButton, reloadButton, codeButton;
        private ObjectField picker;

        [MenuItem("Window/Project Brain/Script Document")]
        public static void Open() => GetWindow<BrainDocumentWindow>("Brain 문서");

        public void CreateGUI()
        {
            minSize = new Vector2(720, 560);
            var root = rootVisualElement; root.Clear(); BrainTheme.Apply(root);
            root.AddToClassList("document-window");
            root.style.paddingLeft = root.style.paddingRight = 0;
            root.style.paddingTop = root.style.paddingBottom = 0;
            var header = new VisualElement(); header.AddToClassList("doc-header"); root.Add(header);
            var heading = new VisualElement(); heading.AddToClassList("row"); header.Add(heading);
            var names = new VisualElement(); names.style.flexGrow = 1; names.style.minWidth = 0; heading.Add(names);
            names.Add(BrainTheme.Label("SCRIPT DOCUMENT", "eyebrow"));
            documentTitle = BrainTheme.Label("", "doc-title"); names.Add(documentTitle);
            subtitle = BrainTheme.Label("", "single-line"); names.Add(subtitle);
            saveState = BrainTheme.Label("", "save-state"); heading.Add(saveState);
            picker = new ObjectField("스크립트 선택") { name = "document-script", objectType = typeof(MonoScript), allowSceneObjects = false };
            picker.SetValueWithoutNotify(selectedScript);
            picker.RegisterValueChangedCallback(evt =>
            {
                if (!ConfirmDiscard()) { picker.SetValueWithoutNotify(selectedScript); return; }
                Run(() => LoadScript(evt.newValue as MonoScript));
                picker.SetValueWithoutNotify(selectedScript);
            });
            header.Add(picker);
            tabs = new VisualElement { name = "document-tabs" }; tabs.AddToClassList("doc-tabs"); root.Add(tabs);
            form = new ScrollView(ScrollViewMode.Vertical) { name = "document-content" };
            form.AddToClassList("document-page"); form.style.flexGrow = 1; form.style.minWidth = 0; form.style.minHeight = 0; root.Add(form);
            message = new HelpBox("", HelpBoxMessageType.Info) { name = "document-message" };
            message.style.display = DisplayStyle.None; root.Add(message);
            var footer = new VisualElement(); footer.AddToClassList("doc-footer"); root.Add(footer);
            completeness = BrainTheme.Label("", "muted"); completeness.style.flexGrow = 1; footer.Add(completeness);
            reloadButton = BrainTheme.Button("다시 읽기", () =>
            {
                if (ConfirmDiscard()) Run(() => LoadScript(selectedScript));
            }, "document-reload"); footer.Add(reloadButton);
            codeButton = BrainTheme.Button("코드 열기", () => Run(() =>
            {
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(service.ResolvePath(document));
                if (script == null) throw new InvalidOperationException("연결된 코드가 없습니다.");
                AssetDatabase.OpenAsset(script);
            }), "document-open-code"); footer.Add(codeButton);
            saveButton = BrainTheme.Button("문서 저장", SaveChanges, "document-save");
            saveButton.AddToClassList("primary"); footer.Add(saveButton);
            var note = BrainTheme.Label("기존 문서 편집 · Explorer 사본과 자동 동기화되지 않습니다. 저장은 검증·사람 확인과 별개입니다.", "storage-note");
            root.Add(note);
            if (document == null && selectedScript != null) Run(() => document = service.LoadOrCreate(selectedScript));
            BuildForm(); SetDirty(dirty);
        }

        private void LoadScript(MonoScript script)
        {
            // Read first. A failed read must not discard the current document or its draft.
            var next = script == null ? null : service.LoadOrCreate(script);
            selectedScript = script; document = next; SetDirty(false); BuildForm();
        }

        private void BuildForm()
        {
            if (form == null) return;
            form.Clear(); tabs.Clear();
            Repaint();
            documentTitle.text = document == null ? "설계 문서" : Path.GetFileNameWithoutExtension(service.ResolvePath(document));
            if (document != null && string.IsNullOrEmpty(documentTitle.text)) documentTitle.text = "연결된 코드 없음";
            subtitle.text = document == null ? "프로젝트의 C# 스크립트를 선택해 문서를 작성하세요." : service.ResolvePath(document);
            subtitle.tooltip = subtitle.text;
            var tabNames = new[] { "문서 내용", "첨부 이미지", "연결 코드", "관계도" };
            for (int i = 0; i < tabNames.Length; i++)
            {
                int index = i; var name = tabNames[i];
                if (document != null && i == 1) name += "  " + (document.imageGuids?.Length ?? 0);
                if (document != null && i == 2) name += "  " + (document.relatedScriptGuids?.Length ?? 0);
                var tab = BrainTheme.Button(name, () => { activeTab = index; BuildForm(); }, "document-tab-" + i);
                tab.AddToClassList("quiet"); tab.EnableInClassList("active", activeTab == i);
                tab.SetEnabled(document != null); tabs.Add(tab);
            }
            UpdateStatus();
            if (document == null)
            {
                Section("코드에 설계 맥락을 남기세요", "역할과 설계 이유, 주의사항을 작성하고 이미지와 관련 코드를 연결할 수 있습니다.");
                form.Add(BrainTheme.Label("1  상단에서 스크립트 선택\n\n2  문서 작성 및 자료 연결\n\n3  문서 저장", "empty-guide"));
                return;
            }
            if (activeTab == 0)
            {
                Section("역할과 설계", "무엇을 하는 코드인지, 왜 이렇게 구현했는지 다음 작업자가 이해할 수 있게 작성하세요.");
                AddText("역할", "이 코드가 담당하는 책임", "document-role", document.role, value => document.role = value, 82);
                AddText("설계 의도", "구현 이유와 선택한 방식", "document-design", document.designIntent, value => document.designIntent = value, 100);
                AddText("주의사항", "제약, 예외와 수정할 때 확인할 점", "document-cautions", document.cautions, value => document.cautions = value, 82);
                AddText("상세 본문", "동작 흐름, 사용 예와 추가 설명", "document-body", document.body, value => document.body = value, 180);
                var metadata = new Foldout { text = "문서 정보", value = false };
                metadata.Add(BrainTheme.Label("GUID  " + document.scriptGuid, "muted"));
                metadata.Add(BrainTheme.Label("저장 위치  .projectbrain/docs/" + document.scriptGuid + ".json", "muted"));
                form.Add(metadata);
            }
            else if (activeTab == 1) BuildImages();
            else if (activeTab == 2) BuildRelations();
            else BuildGraph();
            form.scrollOffset = Vector2.zero;
        }

        private void Section(string heading, string description)
        {
            form.Add(BrainTheme.Label(heading, "section-title"));
            form.Add(BrainTheme.Label(description, "section-description"));
        }

        private void AddText(string label, string hint, string name, string value, Action<string> assign, int height)
        {
            var field = new TextField(label) { name = name, multiline = true, value = value ?? "" };
            field.tooltip = hint; field.textEdition.placeholder = hint;
            BrainTheme.WrapField(field, height);
            field.RegisterValueChangedCallback(evt => { assign(evt.newValue); SetDirty(true); });
            form.Add(field);
        }

        private void BuildGraph()
        {
            Section("직접 연결된 문서 관계", "현재 문서에서 연결했거나 이 문서를 참조하는 코드입니다. 노드를 누르면 해당 문서로 이동합니다.");
            // Build only after the complete relationship read succeeds.
            try
            {
                var related = service.GetGraphRelatedGuids(document);
                form.Add(new DocumentGraphView(document, related, script =>
                {
                    if (script == selectedScript || !ConfirmDiscard()) return;
                    Run(() => { LoadScript(script); picker.SetValueWithoutNotify(selectedScript); });
                }));
                if (related.Length == 0) form.Add(BrainTheme.Label("연결 코드 탭에서 다른 코드를 추가해보세요.", "muted"));
            }
            catch (Exception e)
            {
                form.Add(new HelpBox("관계를 읽지 못했습니다. 문서 편집 내용은 유지됩니다.\n" + e.Message, HelpBoxMessageType.Error));
                form.Add(BrainTheme.Button("관계 다시 읽기", BuildForm, "document-graph-retry"));
            }
        }

        private VisualElement ResourceCard(string heading, string path)
        {
            var card = new VisualElement(); card.AddToClassList("resource-card");
            var label = BrainTheme.Label(heading, "resource-title"); label.tooltip = heading; card.Add(label);
            var location = BrainTheme.Label(path, "single-line"); location.tooltip = path; card.Add(location);
            form.Add(card); return card;
        }

        private void BuildImages()
        {
            Section("첨부 이미지", "설계도와 참고 화면을 문서에 연결합니다. 제거는 연결만 해제하며 이미지 파일은 유지합니다.");
            var add = new ObjectField("이미지 추가") { name = "document-add-image", objectType = typeof(Texture2D), allowSceneObjects = false };
            add.RegisterValueChangedCallback(evt => Run(() =>
            {
                if (evt.newValue == null) return;
                var path = AssetDatabase.GetAssetPath(evt.newValue);
                if (!path.StartsWith("Assets/", StringComparison.Ordinal)) throw new InvalidOperationException("Assets 안의 이미지를 선택하세요.");
                var guid = AssetDatabase.AssetPathToGUID(path);
                document.imageGuids = (document.imageGuids ?? Array.Empty<string>()).Append(guid).Distinct().ToArray();
                SetDirty(true); BuildForm();
            }));
            form.Add(add);
            if ((document.imageGuids?.Length ?? 0) == 0) form.Add(BrainTheme.Label("아직 첨부한 이미지가 없습니다.", "empty-guide"));
            foreach (var guid in document.imageGuids ?? Array.Empty<string>())
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                var card = ResourceCard(texture == null ? "이미지 파일 없음" : Path.GetFileName(path), path.Length == 0 ? guid : path);
                if (texture != null)
                {
                    card.Add(new Image { image = texture, scaleMode = ScaleMode.ScaleToFit, style = { height = 230, marginTop = 12, marginBottom = 12 } });
                    card.Add(BrainTheme.Button("프로젝트에서 찾기", () => EditorGUIUtility.PingObject(texture)));
                }
                card.Add(BrainTheme.Button("이미지 연결 제거", () =>
                {
                    document.imageGuids = document.imageGuids.Where(id => id != guid).ToArray();
                    SetDirty(true); BuildForm();
                }));
            }
        }

        private void BuildRelations()
        {
            Section("연결 코드", "함께 이해해야 할 코드와 문서를 관리합니다. 여기서 추가한 연결은 문서 저장 시 반영됩니다.");
            var add = new ObjectField("코드 추가") { name = "document-add-code", objectType = typeof(MonoScript), allowSceneObjects = false };
            add.RegisterValueChangedCallback(evt => Run(() =>
            {
                if (evt.newValue == null) return;
                var related = service.LoadOrCreate(evt.newValue as MonoScript);
                if (related.scriptGuid == document.scriptGuid) throw new InvalidOperationException("다른 코드를 선택하세요.");
                document.relatedScriptGuids = (document.relatedScriptGuids ?? Array.Empty<string>()).Append(related.scriptGuid).Distinct().ToArray();
                SetDirty(true); BuildForm();
            }));
            form.Add(add);
            if ((document.relatedScriptGuids?.Length ?? 0) == 0) form.Add(BrainTheme.Label("아직 직접 연결한 코드가 없습니다. 들어오는 연결은 관계도에서 확인할 수 있습니다.", "empty-guide"));
            foreach (var guid in document.relatedScriptGuids ?? Array.Empty<string>())
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                var card = ResourceCard(script == null ? "코드 파일 없음" : Path.GetFileNameWithoutExtension(path), path.Length == 0 ? guid : path);
                var row = new VisualElement(); row.AddToClassList("row"); card.Add(row);
                var open = BrainTheme.Button("문서 열기", () =>
                {
                    if (ConfirmDiscard()) Run(() => { LoadScript(script); picker.SetValueWithoutNotify(selectedScript); });
                }); open.SetEnabled(script != null); row.Add(open);
                var code = BrainTheme.Button("코드 열기", () => AssetDatabase.OpenAsset(script)); code.SetEnabled(script != null); row.Add(code);
                row.Add(BrainTheme.Button("연결 제거", () =>
                {
                    document.relatedScriptGuids = document.relatedScriptGuids.Where(id => id != guid).ToArray();
                    SetDirty(true); BuildForm();
                }));
            }
        }

        private bool ConfirmDiscard() => !dirty || EditorUtility.DisplayDialog("미저장 문서", "저장하지 않은 변경을 버릴까요?", "변경 버리기", "취소");
        private void UpdateStatus()
        {
            if (saveState == null) return;
            saveState.text = document == null ? "선택 대기" : dirty ? "저장하지 않은 변경" : string.IsNullOrEmpty(document.updatedUtc) ? "새 문서" : "저장된 문서";
            saveState.EnableInClassList("unsaved", dirty);
            saveButton.SetEnabled(document != null);
            reloadButton.SetEnabled(document != null && selectedScript != null);
            codeButton.SetEnabled(document != null && selectedScript != null);
            int filled = document == null ? 0 : new[] { document.role, document.designIntent, document.cautions, document.body }.Count(v => !string.IsNullOrWhiteSpace(v));
            completeness.text = document == null ? "스크립트를 선택하세요" : "작성 항목 " + filled + " / 4 · 내용의 정확성은 직접 확인하세요";
        }
        private void SetDirty(bool value)
        {
            dirty = value; hasUnsavedChanges = value;
            if (message != null) { message.text = ""; message.style.display = DisplayStyle.None; }
            saveChangesMessage = "Brain 문서의 변경을 저장할까요?";
            UpdateStatus();
        }
        public override void SaveChanges() => Run(() =>
        {
            if (document == null) return;
            service.Save(document); SetDirty(false);
            message.text = "문서를 저장했습니다."; message.messageType = HelpBoxMessageType.Info;
        });
        public override void DiscardChanges() { SetDirty(false); base.DiscardChanges(); }
        private void Run(Action action)
        {
            try { action(); }
            catch (Exception e) { if (message != null) { message.text = e.Message; message.messageType = HelpBoxMessageType.Error; } Debug.LogWarning("[Project Brain] " + e.Message); }
            finally { if (message != null) message.style.display = string.IsNullOrEmpty(message.text) ? DisplayStyle.None : DisplayStyle.Flex; }
        }
    }
}
