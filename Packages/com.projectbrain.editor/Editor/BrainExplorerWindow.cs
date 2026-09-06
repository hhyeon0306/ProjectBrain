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
        private readonly UnityBrainJson json = new UnityBrainJson();
        [MenuItem("Window/Project Brain/Explorer")]
        public static void Open() => GetWindow<BrainExplorerWindow>("Brain Explorer");
        public void CreateGUI()
        {
            hasUnsavedChanges = taskDirty;
            saveChangesMessage = "Brain 작업 요약의 변경을 저장할까요?";
            minSize = new Vector2(760, 500);
            var root = rootVisualElement; root.Clear(); root.style.paddingLeft = root.style.paddingRight = 10;
            root.Add(new Label("PROJECT BRAIN · 코드와 작업 기억") { style = { fontSize = 18, marginTop = 10, marginBottom = 8 } });
            root.Add(new Button(BrainTaskWindow.Open) { text = "작업 관리 · 범위와 종료 기록" });
            root.Add(new Button(CreateGUI) { text = "자료 새로 읽기" });
            message = new HelpBox("저장은 검증·사람 확인·완료 승인이 아닙니다.", HelpBoxMessageType.Info); root.Add(message);
            Run(() =>
            {
                graph = new BrainGraphService(new BrainStore(Path.Combine(ScriptDocumentService.ProjectRoot, ".projectbrain"), json));
                var columns = new TwoPaneSplitView(0, 240, TwoPaneSplitViewOrientation.Horizontal); columns.style.flexGrow = 1;
                var tree = new ScrollView(); tree.style.minWidth = 180; columns.Add(tree);
                foreach (var node in graph.Nodes.Values.Where(n => n.type == "Project").OrderBy(n => n.id, StringComparer.Ordinal)) AddTree(tree, node.id, 0);
                tree.Add(new Label("기존 문서·이미지 (Player 전용 아님)"));
                foreach (var node in graph.Nodes.Values.Where(n => n.type == "Document" || n.type == "Image").OrderBy(n => n.title, StringComparer.Ordinal))
                    tree.Add(Link(node.type + " · " + node.title, () => SelectNode(node.id)));
                var right = new VisualElement(); right.style.flexGrow = 1; right.style.minWidth = 340;
                detail = new ScrollView(); detail.style.flexGrow = 1; right.Add(detail);
                taskPanel = new ScrollView(); taskPanel.style.maxHeight = 240; right.Add(taskPanel); columns.Add(right); root.Add(columns);
                if (!graph.Nodes.ContainsKey(selectedId)) selectedId = graph.Nodes.Keys.OrderBy(id => id, StringComparer.Ordinal).FirstOrDefault();
                if (selectedId != null) SelectNode(selectedId);
                BuildTask();
            });
        }
        private void AddTree(VisualElement parent, string id, int level)
        {
            var button = Link(graph.Get(id).title, () => SelectNode(id)); button.style.marginLeft = level * 14; parent.Add(button);
            foreach (var child in graph.Children(id)) AddTree(parent, child, level + 1);
        }
        public void SelectNode(string id) => Run(() =>
        {
            var node = graph.Get(id); selectedId = id; detail.Clear();
            var parent = graph.Parent(id);
            if (parent != null) detail.Add(new Button(() => SelectNode(parent)) { text = "← " + graph.Get(parent).title });
            detail.Add(new Label(node.title) { style = { fontSize = 18, whiteSpace = WhiteSpace.Normal } });
            Text(node.type + " · " + id); Text(node.summary);
            var freshness = new BrainFreshnessService(ScriptDocumentService.ProjectRoot, json, AssetDatabase.GUIDToAssetPath).Inspect(graph, id);
            Text("최신성: " + freshness.state + " · 검증 결과는 Evidence와 작업 상태에서 확인");
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
            if ((node.type == "Feature" || node.type == "Code") && !related.Any(r => r.type == "documented_by" && r.from == id)) Text("직접 연결된 설계 문서가 없습니다. 관련 코드의 문서도 확인하세요.");
            foreach (var r in related)
            {
                var next = r.from == id ? r.to : r.from;
                detail.Add(Link((r.from == id ? "→ " : "← ") + r.type + " · " + graph.Get(next).title, () => SelectNode(next)));
                Text("출처: " + r.source);
            }
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
        private void Run(Action action) { try { action(); } catch (Exception e) { if (message != null) { message.text = e.Message; message.messageType = HelpBoxMessageType.Error; } } }
    }
}
