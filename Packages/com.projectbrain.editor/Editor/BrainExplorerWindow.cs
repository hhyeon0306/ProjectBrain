using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectBrain
{
    public sealed class BrainExplorerWindow : EditorWindow
    {
        [SerializeField] private string selectedId = "project:brain-demo";
        [SerializeField] private bool taskDirty;
        [SerializeField] private string draftTaskId;
        [SerializeField] private int draftRevision;
        [SerializeField] private string draftProgress;
        [SerializeField] private string draftDecisions;
        [SerializeField] private string draftUnresolved;
        [SerializeField] private string draftNext;
        [SerializeField] private string[] draftReferences;
        private BrainGraphService graph;
        private HelpBox message;
        private ScrollView detail;
        private ScrollView taskPanel;
        private BrainTaskRecord task;
        private BrainMapView map;
        private VisualElement inspector, summaryDrawer, filterPanel;
        private Label mapCount, zoomLabel, taskLabel;
        [SerializeField] private bool localView;
        [SerializeField] private string searchText = "";
        private readonly UnityBrainJson json = new UnityBrainJson();
        [MenuItem("Window/Project Brain/Explorer")]
        public static void Open() => GetWindow<BrainExplorerWindow>("Brain Explorer");
        public void CreateGUI()
        {
            hasUnsavedChanges = taskDirty;
            saveChangesMessage = "Brain 작업 요약의 변경을 저장할까요?";
            minSize = new Vector2(860, 600);
            var root = rootVisualElement; root.Clear(); BrainTheme.Apply(root);
            root.style.paddingLeft = root.style.paddingRight = 0;
            var bar = new VisualElement(); bar.AddToClassList("bar"); root.Add(bar);
            bar.Add(BrainTheme.Label("◈  Brain / Project map", "brand"));
            var space = new VisualElement(); space.style.flexGrow = 1; bar.Add(space);
            var search = new TextField { name = "map-search", value = searchText, tooltip = "제목·설명·ID로 노드를 검색합니다." };
            search.textEdition.placeholder = "노드·문서 검색"; search.AddToClassList("search"); bar.Add(search);
            search.RegisterValueChangedCallback(e => { searchText = e.newValue; map?.SetQuery(searchText); });
            bar.Add(BrainTheme.Button("작업 관리", BrainTaskWindow.Open, "open-tasks"));
            bar.Add(BrainTheme.Button("기존 문서", BrainDocumentWindow.Open, "open-documents"));
            bar.Add(BrainTheme.Button("새로 읽기", CreateGUI, "map-refresh"));
            message = new HelpBox("", HelpBoxMessageType.Info); message.style.display = DisplayStyle.None; root.Add(message);
            Run(() =>
            {
                graph = new BrainGraphService(new BrainStore(Path.Combine(ScriptDocumentService.ProjectRoot, ".projectbrain"), json));
                var canvas = new VisualElement { name = "map-workspace" }; canvas.style.flexGrow = 1; canvas.style.overflow = Overflow.Hidden; root.Add(canvas);
                map = new BrainMapView(graph, SelectNode); canvas.Add(map);
                var heading = new VisualElement { pickingMode = PickingMode.Ignore }; heading.AddToClassList("map-heading");
                heading.Add(BrainTheme.Label("Project map", "headline"));
                heading.Add(BrainTheme.Label("기능과 코드, 문서를 연결해서 탐색합니다", "muted")); canvas.Add(heading);
                mapCount = BrainTheme.Label("", "map-count"); mapCount.pickingMode = PickingMode.Ignore; canvas.Add(mapCount);
                var modes = new VisualElement(); modes.AddToClassList("map-modes"); canvas.Add(modes);
                Button all = null, nearby = null;
                Action<bool> mode = value => { localView = value; map.SetLocal(value); all.EnableInClassList("active", !value); nearby.EnableInClassList("active", value); };
                all = BrainTheme.Button("전체", () => mode(false), "map-all"); nearby = BrainTheme.Button("선택 주변", () => mode(true), "map-local");
                nearby.tooltip = "선택한 노드에서 두 단계 이내의 관계를 표시합니다."; modes.Add(all); modes.Add(nearby);
                inspector = new VisualElement { name = "map-inspector" }; inspector.AddToClassList("floating"); inspector.AddToClassList("inspector"); canvas.Add(inspector);
                detail = new ScrollView(); detail.style.flexGrow = 1; inspector.Add(detail);
                var toolbar = new VisualElement(); toolbar.AddToClassList("floating"); toolbar.AddToClassList("map-tools"); canvas.Add(toolbar);
                toolbar.Add(BrainTheme.Button("맞춤", () => map.Fit(), "map-fit"));
                toolbar.Add(BrainTheme.Button("−", () => map.ZoomBy(1 / 1.2f), "map-zoom-out"));
                zoomLabel = new Label("100%"); toolbar.Add(zoomLabel);
                toolbar.Add(BrainTheme.Button("+", () => map.ZoomBy(1.2f), "map-zoom-in"));
                toolbar.Add(BrainTheme.Button("필터", () => filterPanel.style.display = filterPanel.resolvedStyle.display == DisplayStyle.None ? DisplayStyle.Flex : DisplayStyle.None, "map-filter"));
                toolbar.Add(BrainTheme.Button("작업 기억", () => summaryDrawer.style.display = summaryDrawer.resolvedStyle.display == DisplayStyle.None ? DisplayStyle.Flex : DisplayStyle.None, "map-summary"));
                filterPanel = new VisualElement { name = "map-filters" }; filterPanel.AddToClassList("floating"); filterPanel.AddToClassList("filter-panel"); filterPanel.style.display = DisplayStyle.None; canvas.Add(filterPanel);
                filterPanel.Add(BrainTheme.Label("표시할 노드", "eyebrow"));
                foreach (var type in graph.Nodes.Values.Select(n => n.type).Distinct().OrderBy(t => t))
                {
                    var toggle = new Toggle(BrainTheme.TypeName(type)) { value = true, name = "filter-" + type };
                    toggle.RegisterValueChangedCallback(e => map.ShowType(type, e.newValue)); filterPanel.Add(toggle);
                }
                summaryDrawer = new VisualElement { name = "map-summary-drawer" }; summaryDrawer.AddToClassList("floating"); summaryDrawer.AddToClassList("summary-panel"); summaryDrawer.style.display = taskDirty ? DisplayStyle.Flex : DisplayStyle.None; canvas.Add(summaryDrawer);
                summaryDrawer.Add(BrainTheme.Button("작업 기억 닫기", () => summaryDrawer.style.display = DisplayStyle.None));
                taskPanel = new ScrollView(); taskPanel.style.flexGrow = 1; summaryDrawer.Add(taskPanel);
                var legend = BrainTheme.Label("● 기능·계층   □ 코드   ▤ 문서   ◇ 기록    ·    드래그 이동 / 휠 확대", "map-legend"); legend.pickingMode = PickingMode.Ignore; canvas.Add(legend);
                map.Changed = () => { zoomLabel.text = Mathf.RoundToInt(map.Zoom * 100) + "%"; mapCount.text = map.VisibleCount == 0 ? "검색·필터에 맞는 노드가 없습니다." : map.VisibleCount + " / " + graph.Nodes.Count + " 노드 · 저장된 관계"; };
                var footer = new VisualElement(); footer.AddToClassList("bar"); footer.style.minHeight = 30; root.Add(footer);
                taskLabel = BrainTheme.Label("", "muted"); taskLabel.style.flexGrow = 1; footer.Add(taskLabel);
                footer.Add(BrainTheme.Button("작업 관리 ↗", BrainTaskWindow.Open));
                if (!graph.Nodes.ContainsKey(selectedId)) selectedId = graph.Nodes.Keys.OrderBy(id => id, StringComparer.Ordinal).FirstOrDefault();
                if (selectedId != null) SelectNode(selectedId);
                map.SetQuery(searchText); mode(localView);
                BuildTask();
            });
        }
        public void SelectNode(string id) => Run(() =>
        {
            var node = graph.Get(id); selectedId = id; detail.Clear(); map?.SetSelected(id);
            detail.Add(BrainTheme.Label(BrainTheme.TypeName(node.type).ToUpperInvariant(), "eyebrow"));
            var parent = graph.Parent(id);
            if (parent != null) { var back = new Button(() => SelectNode(parent)) { text = "← " + graph.Get(parent).title }; back.AddToClassList("quiet"); detail.Add(back); }
            detail.Add(BrainTheme.Label(node.title, "detail-title"));
            Text(node.summary);
            var freshness = new BrainFreshnessService(ScriptDocumentService.ProjectRoot, json, AssetDatabase.GUIDToAssetPath).Inspect(graph, id);
            var metadata = new Foldout { text = "자료 정보 · " + freshness.state, value = false };
            metadata.Add(BrainTheme.Label(id, "muted")); metadata.Add(BrainTheme.Label("최신성은 저장 당시 코드와의 일치 여부입니다. 검증 통과나 사람 확인과는 별개입니다.", "muted"));
            if (node.type == "Evidence")
            {
                var record = new BrainVerificationStore(ScriptDocumentService.ProjectRoot, json).Load(node.id.Substring("evidence:".Length));
                var current = new BrainVerificationStore(ScriptDocumentService.ProjectRoot, json).CurrentPass(record, new BrainVerificationStore(ScriptDocumentService.ProjectRoot, json).Snapshot());
                Text(record.kind + " · " + record.state + " · 현재 통과 근거: " + current);
                Text("전체 " + record.total + " / 통과 " + record.passed + " / 실패 " + record.failed + " / 건너뜀 " + record.skipped); Text(record.summary); Text(node.body);
            }
            if (node.type == "Activity") Text(node.body);
            if (node.type == "Image")
            {
                var image = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(node.assetGuid));
                if (image == null) Text("이미지 파일이 없습니다.");
                else detail.Add(new Image { image = image, scaleMode = ScaleMode.ScaleToFit, style = { height = 190 } });
            }
            if (node.type == "Document")
            {
                detail.Add(new TextField("본문 (읽기 전용)") { value = node.body, multiline = true, isReadOnly = true, style = { whiteSpace = WhiteSpace.Normal } });
                Text("이 화면은 nodes를 읽습니다. 기존 문서 창의 docs 수정은 여기에 자동 반영되지 않습니다.");
                AddReview(node.id);
            }
            if (node.type == "Code") detail.Add(new Button(() => Run(() =>
            {
                var asset = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(node.assetGuid));
                if (asset == null) throw new InvalidDataException("코드 자산이 없습니다."); AssetDatabase.OpenAsset(asset);
            })) { text = "코드 열기" });
            var related = graph.Around(id);
            if (node.type == "Feature" || node.type == "Code")
            {
                var codeIds = node.type == "Code" ? new[] { id } : related.Where(r => r.from == id && r.type == "implemented_by").Select(r => r.to).ToArray();
                var documents = graph.Relations.Where(r => r.type == "documented_by" && (r.from == id || codeIds.Contains(r.from))).Select(r => r.to).Distinct().ToArray();
                if (documents.Length == 0) Text("연결된 설계 문서가 없습니다.");
                foreach (var document in documents)
                {
                    var open = Link("문서 열기 ↗  " + graph.Get(document).title, () => SelectNode(document)); open.AddToClassList("primary"); detail.Add(open);
                }
            }
            foreach (var r in related)
            {
                var next = r.from == id ? r.to : r.from;
                var link = Link((r.from == id ? "→ " : "← ") + BrainTheme.RelationName(r.type) + " · " + graph.Get(next).title, () => SelectNode(next));
                link.AddToClassList("quiet");
                link.tooltip = r.from + " → " + r.to + "\n" + r.type + " · 출처: " + r.source; detail.Add(link);
            }
            detail.Add(metadata);
        });
        private void Text(string text) => detail.Add(new Label(text) { style = { whiteSpace = WhiteSpace.Normal, marginBottom = 5 } });
        private void AddReview(string documentId)
        {
            var active = new BrainTaskService(ScriptDocumentService.ProjectRoot, json).Load();
            if (active == null) { Text("문서 확인은 작업 시작 후 가능합니다."); return; }
            var service = new BrainCompletionService(ScriptDocumentService.ProjectRoot, json, AssetDatabase.GUIDToAssetPath);
            var review = service.InspectDocument(documentId, json.Write(graph.Get(documentId)));
            Text("사람 문서 확인: " + review.state + " · 연결 코드: " + string.Join(", ", review.codePaths));
            var acknowledged = new Toggle("현재 본문과 연결 코드를 직접 읽고 내용이 맞는지 확인했습니다.");
            acknowledged.style.whiteSpace = WhiteSpace.Normal; detail.Add(acknowledged);
            var button = new Button(() => Run(() =>
            {
                service.ConfirmFromHuman(active.id, active.revision, documentId, review.snapshotHash);
                SelectNode(documentId); message.text = "사람 확인을 기록했습니다. 코드 검증·작업 완료와는 별개입니다.";
                message.messageType = HelpBoxMessageType.Info;
            })) { text = "사람 확인 기록" };
            button.SetEnabled(false);
            acknowledged.RegisterValueChangedCallback(e => button.SetEnabled(e.newValue && review.state != "missing-code"));
            detail.Add(button);
        }
        private void BuildTask()
        {
            taskPanel.Clear(); task = new BrainTaskService(ScriptDocumentService.ProjectRoot, json).Load();
            if (taskLabel != null) taskLabel.text = task == null ? "진행 중인 작업 없음 · 작업 기억에서 시작" : "현재 작업   " + task.purpose;
            taskPanel.Add(new Label("작업 기억"));
            if (task == null)
            {
                var purpose = new TextField("작업 목적"); taskPanel.Add(purpose);
                var allowed = new TextField("허용 경로") { value = "Assets/Scripts/" }; taskPanel.Add(allowed);
                taskPanel.Add(new Button(() => Run(() => { new BrainTaskService(ScriptDocumentService.ProjectRoot, json).Begin(purpose.value, new[] { selectedId }, new[] { allowed.value }); BuildTask(); })) { text = "선택 노드에서 작업 시작" });
                return;
            }
            if (!taskDirty)
            {
                draftTaskId = task.id; draftRevision = task.revision; draftReferences = task.references;
                draftProgress = task.progress; draftDecisions = task.decisions; draftUnresolved = task.unresolved; draftNext = task.nextAction;
            }
            taskPanel.Add(new Label(task.purpose + " · revision " + task.revision) { style = { whiteSpace = WhiteSpace.Normal } });
            taskPanel.Add(new Button(() => Run(() =>
            {
                var result = new BrainCompletionService(ScriptDocumentService.ProjectRoot, json, AssetDatabase.GUIDToAssetPath).Check(task.id, task.revision);
                message.text = (result.ready ? "완료 가능 (아직 완료 기록 전)" : "완료 거절") + "\n" + string.Join("\n", result.reasons.GroupBy(r => r.code).Select(g => g.Key + " (" + g.Count() + "건) · " + g.First().nextAction));
                message.messageType = HelpBoxMessageType.Warning;
            })) { text = "완료 조건 확인" });
            foreach (var kind in new[] { "compile", "editmode" })
                taskPanel.Add(new Button(() => Run(() => { var run = BrainUnityVerification.Start(task.id, task.revision, kind); message.text = kind + " 실행 중 · " + run.id + " · 완료 조건 확인으로 결과를 조회하세요."; })) { text = kind + " 검증 실행" });
            Field("진행", draftProgress, v => draftProgress = v); Field("결정·근거", draftDecisions, v => draftDecisions = v);
            Field("미해결", draftUnresolved, v => draftUnresolved = v); Field("다음 행동", draftNext, v => draftNext = v);
            taskPanel.Add(new Button(() => SaveChanges()) { text = "요약 저장" });
            taskPanel.Add(new Button(() => Run(() =>
            {
                var state = new BrainTaskService(ScriptDocumentService.ProjectRoot, json).Status();
                message.text = "변경 " + state.changes.Length + "개 · 허용 밖 " + state.changes.Count(c => !c.allowed) + "개 · coverage 제한 " + state.coverageLimitations.Length + "개";
                message.messageType = HelpBoxMessageType.Info;
            })) { text = "변경 다시 확인" });
        }
        public override void SaveChanges() => Run(() =>
        {
            if (draftTaskId == null) return;
            new BrainTaskService(ScriptDocumentService.ProjectRoot, json).Update(draftTaskId, draftRevision, draftProgress, draftDecisions, draftUnresolved, draftNext, draftReferences);
            taskDirty = false; hasUnsavedChanges = false; BuildTask();
            message.text = "작업 요약을 저장했습니다. 기준선·검증 상태는 갱신하지 않았습니다."; message.messageType = HelpBoxMessageType.Info;
        });
        public override void DiscardChanges() { taskDirty = false; hasUnsavedChanges = false; base.DiscardChanges(); }
        private void Field(string label, string value, Action<string> assign)
        {
            var field = new TextField(label) { value = value, multiline = true, style = { whiteSpace = WhiteSpace.Normal } };
            field.RegisterValueChangedCallback(evt => { assign(evt.newValue); taskDirty = true; hasUnsavedChanges = true; });
            taskPanel.Add(field);
        }
        private static Button Link(string text, Action action) => new Button(action) { text = text, tooltip = text, style = { whiteSpace = WhiteSpace.Normal, height = StyleKeyword.Auto, minHeight = 24, unityTextAlign = TextAnchor.MiddleLeft } };
        private void Run(Action action) { try { action(); } catch (Exception e) { if (message != null) { message.text = e.Message; message.messageType = HelpBoxMessageType.Error; } } finally { if (message != null) message.style.display = string.IsNullOrEmpty(message.text) ? DisplayStyle.None : DisplayStyle.Flex; } }
    }
}
