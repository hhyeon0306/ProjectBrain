using System;
using System.Collections.Generic;
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
        [SerializeField] private string loadedVersion;
        [SerializeField] private bool editing;
        [SerializeField] private string readingId = "";
        [SerializeField] private string category = "Project";
        [SerializeField] private bool dependencies;
        [SerializeField] private string readerSearch = "";
        [Serializable] private sealed class ReaderLocation
        {
            public string id, category, search;
            public bool dependencies;
            public Vector2 scroll;
        }
        [SerializeField] private List<ReaderLocation> readingHistory = new List<ReaderLocation>();
        [SerializeField] private Vector2 readerScroll;
        private ScrollView readerPage;
        private readonly ScriptDocumentService service = new ScriptDocumentService();
        private ScrollView form;
        private VisualElement tabs;
        private Label documentTitle, subtitle, saveState, completeness;
        private HelpBox message;
        private Button saveButton, reloadButton, codeButton;
        private ObjectField picker;

        [MenuItem("Window/Project Brain/Script Document")]
        public static void Open() { var window = GetWindow<BrainDocumentWindow>("설계 문서"); window.editing = false; window.CreateGUI(); }
        public static void OpenScript(MonoScript script)
        {
            if (script != null) OpenNode("asset:" + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(script)));
        }
        public static void OpenNode(string id, bool showDependencies = false)
        {
            var window = GetWindow<BrainDocumentWindow>("설계 문서");
            window.NavigateReader(id, showDependencies); window.Focus();
        }
        private void NavigateReader(string id, bool showDependencies = false, string nextCategory = null)
        {
            if (readingId != id || dependencies != showDependencies || nextCategory != null && category != nextCategory)
            {
                if (!string.IsNullOrEmpty(readingId))
                {
                    readingHistory.Add(new ReaderLocation { id = readingId, category = category, search = readerSearch,
                        dependencies = dependencies, scroll = readerPage == null ? readerScroll : readerPage.scrollOffset });
                    if (readingHistory.Count > 50) readingHistory.RemoveAt(0);
                }
                readerScroll = Vector2.zero;
            }
            readingId = id; dependencies = showDependencies;
            if (nextCategory != null) category = nextCategory;
            editing = false; CreateGUI();
        }
        private void GoBack()
        {
            if (readingHistory.Count == 0) return;
            var previous = readingHistory[readingHistory.Count - 1]; readingHistory.RemoveAt(readingHistory.Count - 1);
            readingId = previous.id; category = previous.category; readerSearch = previous.search;
            dependencies = previous.dependencies; readerScroll = previous.scroll; editing = false; CreateGUI();
        }
        private void OpenEditor(MonoScript script)
        {
            var window = this;
            if (window.selectedScript != script && !window.ConfirmDiscard()) return;
            window.editing = true;
            window.CreateGUI();
            if (window.form == null) window.CreateGUI();
            if (window.selectedScript == script && window.document != null) { window.Focus(); return; }
            window.Run(() => { window.LoadScript(script); window.picker.SetValueWithoutNotify(window.selectedScript); });
            window.Focus();
        }

        private void BuildReader()
        {
            var root = rootVisualElement;
            var page = BrainPresentation.Shell(root, "설계 문서", "현재 구조와 설계 이유를 읽습니다. 변경 과정은 작업·검증 기록에서 확인하세요.", out var nav, CreateGUI);
            readerPage = page;
            var restoreScroll = readerScroll;
            bool restoring = restoreScroll != Vector2.zero;
            Action restore = () =>
            {
                if (readerPage != page || !restoring || !(page.contentContainer.layout.height > 0) || !(page.contentViewport.layout.height > 0)) return;
                page.scrollOffset = restoreScroll; readerScroll = page.scrollOffset; restoring = false;
            };
            page.RegisterCallback<GeometryChangedEvent>(_ => restore());
            page.contentContainer.RegisterCallback<GeometryChangedEvent>(_ => restore());
            page.schedule.Execute(restore).StartingIn(1);
            page.verticalScroller.valueChanged += value => { if (readerPage == page && !restoring) readerScroll = new Vector2(page.scrollOffset.x, value); };
            try
            {
                var graph = BrainPresentation.Graph();
                if (!string.IsNullOrEmpty(readingId) && graph.Nodes.TryGetValue(readingId, out var selected)) category = selected.type == "Document" || selected.type == "Feature" ? selected.type == "Document" ? "Code" : "Domain" : selected.type;
                nav.Add(BrainPresentation.Text("문서 분류", "reader-caption"));
                foreach (var item in new[] { ("Project", "전체 아키텍처"), ("Domain", "도메인 설계"), ("Code", "코드 문서"), ("Image", "첨부 자료") })
                    BrainPresentation.Nav(nav, item.Item2, category == item.Item1, () => NavigateReader(graph.Nodes.Values.Where(n => n.type == item.Item1).OrderBy(n => n.title).Select(n => n.id).FirstOrDefault(), false, item.Item1));
                if (dirty) BrainPresentation.Action(nav, "미저장 문서 이어서 편집", "입력 중인 내용은 보존됩니다.", () => { editing = true; CreateGUI(); });
                var search = new TextField { name = "reader-search", value = readerSearch }; search.textEdition.placeholder = "문서·코드 검색"; search.AddToClassList("reader-search"); nav.Add(search);
                var list = new VisualElement { name = "reader-library" }; list.AddToClassList("reader-library"); nav.Add(list);
                Action populate = () =>
                {
                    list.Clear();
                    var found = graph.Nodes.Values.Where(n => n.type == category && (string.IsNullOrWhiteSpace(readerSearch) || (n.title + " " + n.summary).IndexOf(readerSearch, StringComparison.OrdinalIgnoreCase) >= 0)).OrderBy(n => n.title).ToArray();
                    foreach (var n in found.Take(60)) BrainPresentation.Nav(list, n.title, readingId == n.id, () => NavigateReader(n.id));
                    if (found.Length == 0) list.Add(BrainPresentation.Text("검색 결과가 없습니다.", "reader-subtitle"));
                    if (found.Length > 60) list.Add(BrainPresentation.Text("검색으로 나머지 " + (found.Length - 60) + "개를 찾아보세요.", "reader-subtitle"));
                };
                search.RegisterValueChangedCallback(e => { readerSearch = e.newValue; populate(); }); populate();
                if (string.IsNullOrEmpty(readingId)) { readingId = graph.Nodes.Values.Where(n => n.type == category).OrderBy(n => n.title).Select(n => n.id).FirstOrDefault(); populate(); }
                var navigation = new VisualElement(); navigation.AddToClassList("reader-breadcrumbs"); page.Add(navigation);
                var back = BrainTheme.Button("← 뒤로", GoBack, "reader-back"); back.SetEnabled(readingHistory.Count > 0); navigation.Add(back);
                if (readingHistory.Count > 0 && graph.Nodes.TryGetValue(readingHistory[readingHistory.Count - 1].id, out var previous))
                    back.tooltip = previous.title + " 페이지로 돌아갑니다.";
                if (!string.IsNullOrEmpty(readingId) && graph.Nodes.ContainsKey(readingId))
                {
                    var parent = graph.Parent(readingId);
                    if (parent != null) navigation.Add(BrainTheme.Button("상위 · " + graph.Get(parent).title, () => NavigateReader(parent)));
                }
                if (string.IsNullOrEmpty(readingId)) { BrainPresentation.Hero(page, "LIBRARY", "등록된 자료가 없습니다", "코드 문서 편집에서 자료를 연결하면 이곳에 표시됩니다."); return; }
                if (!graph.Nodes.ContainsKey(readingId)) { BrainPresentation.Hero(page, "연결 확인 필요", "이 항목은 현재 구조도에 없습니다", "왼쪽 목록에서 현재 문서를 선택하세요. 미저장 편집 내용은 보존됩니다."); return; }
                var node = graph.Get(readingId);
                if (node.type == "Image")
                {
                    BrainPresentation.Hero(page, "RESOURCE", node.title, node.summary);
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(node.assetGuid));
                    if (texture != null) page.Add(new Image { image = texture, scaleMode = ScaleMode.ScaleToFit, style = { height = 400 } });
                    else page.Add(BrainPresentation.Text("이미지 파일을 찾을 수 없습니다."));
                }
                else BrainPresentation.Document(page, graph, readingId, id => NavigateReader(id), OpenEditor, dependencies, show => NavigateReader(readingId, show));
            }
            catch (Exception e) { page.Add(new HelpBox("문서를 읽지 못했습니다. " + e.Message, HelpBoxMessageType.Error)); }
        }

        public void CreateGUI()
        {
            titleContent = new GUIContent("설계 문서");
            minSize = new Vector2(720, 560);
            if (!editing) { BuildReader(); return; }
            var root = rootVisualElement; root.Clear(); BrainTheme.Apply(root);
            root.AddToClassList("document-window");
            root.style.paddingLeft = root.style.paddingRight = 0;
            root.style.paddingTop = root.style.paddingBottom = 0;
            var header = new VisualElement(); header.AddToClassList("doc-header"); root.Add(header);
            header.Add(BrainTheme.Button("← 설계 문서 읽기", () => { editing = false; CreateGUI(); }));
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
            var note = BrainTheme.Label("문서 저장 시 Explorer의 본문·이미지·연결 코드에 자동 반영됩니다. 저장은 검증·사람 확인과 별개입니다.", "storage-note");
            root.Add(note);
            if (document == null && selectedScript != null) Run(() => document = service.LoadOrCreate(selectedScript));
            if (document != null && string.IsNullOrEmpty(loadedVersion)) Run(() => loadedVersion = service.Version(document));
            BuildForm(); SetDirty(dirty);
        }

        private void LoadScript(MonoScript script)
        {
            // Read first. A failed read must not discard the current document or its draft.
            var next = script == null ? null : service.LoadOrCreate(script);
            var version = next == null ? "" : service.Version(next);
            selectedScript = script; document = next; loadedVersion = version; SetDirty(false); BuildForm();
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
            saveButton.SetEnabled(document != null && selectedScript != null);
            if (document != null && selectedScript == null) saveState.text = "연결 코드 없음 · 보존된 문서";
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
            service.Save(document, loadedVersion); loadedVersion = service.Version(document); SetDirty(false);
            message.text = "문서를 저장하고 Explorer에 반영했습니다."; message.messageType = HelpBoxMessageType.Info;
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
