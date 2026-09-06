using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectBrain
{
    // Human-facing controls call the same lifecycle service as MCP.
    public sealed class BrainTaskWindow : EditorWindow
    {
        [Serializable] private sealed class Draft
        {
            public string taskId = "";
            public int revision;
            public string paths = "";
            public string scopeReason = "";
            public string closeReason = "";
            public bool edited;
        }
        private Draft draft;
        private HelpBox message;
        private BrainTaskRecord active;
        private readonly UnityBrainJson json = new UnityBrainJson();
        [SerializeField] private string historyId = "";
        private string Root => ScriptDocumentService.ProjectRoot;
        private string DraftKey => "Brain.TaskWindow.Draft." + BrainWorkspace.Hash(Root);
        private BrainTaskLifecycle Lifecycle => new BrainTaskLifecycle(Root, json, AssetDatabase.GUIDToAssetPath);
        [MenuItem("Window/Project Brain/Task Management")]
        public static void Open() => GetWindow<BrainTaskWindow>("Brain 작업 관리");
        public void CreateGUI()
        {
            minSize = new Vector2(580, 540);
            var root = rootVisualElement; root.Clear(); BrainTheme.Apply(root);
            root.style.paddingLeft = root.style.paddingRight = 14;
            root.Add(new Label("작업 관리") { style = { fontSize = 21, marginTop = 12, marginBottom = 8 } });
            message = new HelpBox("작업 범위와 종료 기록을 관리합니다. 문서 확인과 검증은 탐색 화면에서 진행하세요.", HelpBoxMessageType.Info);
            message.name = "task-message"; root.Add(message);
            var toolbar = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            toolbar.Add(ActionButton("다시 읽기 · 입력 유지", "refresh", CreateGUI));
            toolbar.Add(ActionButton("입력 버리고 최신 작업 읽기", "discard", () => { draft = null; SessionState.EraseString(DraftKey); CreateGUI(); }));
            toolbar.Add(ActionButton("탐색 화면", "explorer", BrainExplorerWindow.Open)); root.Add(toolbar);
            var scroll = new ScrollView { style = { flexGrow = 1 } }; scroll.AddToClassList("task-body"); root.Add(scroll);
            Run(() =>
            {
                active = new BrainTaskService(Root, json).Load();
                if (draft == null) { var saved = SessionState.GetString(DraftKey, ""); draft = saved.Length == 0 ? new Draft() : JsonUtility.FromJson<Draft>(saved) ?? new Draft(); }
                if (!draft.edited) BindActive();
                if (active == null)
                {
                    Label(scroll, "진행 중인 작업이 없습니다. 탐색 화면에서 대상을 선택하고 새 작업을 시작하세요.");
                    if (draft.edited) Label(scroll, "이전 작업의 입력을 보존 중입니다. 입력 버리기를 누르기 전까지 저장되지 않습니다.");
                }
                else BuildActive(scroll);
                BuildHistory(scroll);
            });
        }
        private void BindActive()
        {
            draft = new Draft { taskId = active?.id ?? "", revision = active?.revision ?? 0, paths = active == null ? "" : string.Join("\n", active.allowedPaths) };
            Persist();
        }
        private void Persist() => SessionState.SetString(DraftKey, JsonUtility.ToJson(draft));
        private void BuildActive(VisualElement parent)
        {
            Label(parent, active.purpose, true);
            var metadata = new Foldout { text = "작업 정보 · 버전 " + active.revision, value = false };
            Label(metadata, "작업 ID  " + active.id + "\n시작 기준 파일  " + active.baseline.files.Length + "개");
            parent.Add(metadata);
            if (draft.taskId != active.id || draft.revision != active.revision)
                Label(parent, "다른 곳에서 작업이 바뀌었습니다. 아래 입력은 이전 버전입니다. 필요한 내용을 복사한 뒤 입력 버리고 최신 작업 읽기를 누르세요.");
            var scope = new Foldout { text = "수정할 수 있는 범위", value = true }; parent.Add(scope);
            Label(scope, "한 줄에 경로 하나씩 입력하세요. 폴더 전체는 /로 끝냅니다. 범위를 바꿔도 처음 기준선과 확인할 문서는 유지됩니다.");
            Input(scope, "허용 경로", "scope-paths", draft.paths, v => draft.paths = v);
            Input(scope, "변경 이유", "scope-reason", draft.scopeReason, v => draft.scopeReason = v);
            scope.Add(ActionButton("범위 저장", "save-scope", SaveScope));
            var close = new Foldout { text = "작업 마무리", value = true }; parent.Add(close);
            Label(close, "완료 종료는 문서 확인과 현재 검증 조건을 검사합니다. 미완료 종료는 남은 문제를 기록하고 작업을 닫습니다.");
            Input(close, "종료 이유", "close-reason", draft.closeReason, v => draft.closeReason = v);
            close.Add(ActionButton("완료 조건 확인", "check-completion", () =>
            {
                RequireCurrent(); var result = new BrainCompletionService(Root, json, AssetDatabase.GUIDToAssetPath).Check(draft.taskId, draft.revision);
                Show(result.ready ? "완료 조건을 충족했습니다. 아직 종료하지 않았습니다." : "아직 완료할 수 없습니다.\n" + string.Join("\n", result.reasons.GroupBy(r => r.code).Select(g => g.Count() + "건 · " + g.First().nextAction)), !result.ready);
            }));
            close.Add(ActionButton("조건 확인 후 완료 종료", "close-completed", () => CloseTask("completed")));
            var acknowledge = new Toggle("미해결 사항을 남긴 채 종료합니다. 완료로 기록되지 않습니다.") { name = "acknowledge-abandon" };
            acknowledge.style.whiteSpace = WhiteSpace.Normal; close.Add(acknowledge);
            var abandon = ActionButton("미완료로 종료", "close-abandoned", () => { BrainWorkspace.Require(acknowledge.value, "미완료 종료 여부를 선택하세요."); CloseTask("abandoned"); });
            abandon.SetEnabled(false); acknowledge.RegisterValueChangedCallback(e => abandon.SetEnabled(e.newValue)); close.Add(abandon);
            var changes = new Foldout { text = "범위 변경 이력 · " + active.scopeChanges.Length + "건", value = false }; parent.Add(changes);
            if (active.scopeChanges.Length == 0) Label(changes, "범위를 변경한 기록이 없습니다.");
            foreach (var change in active.scopeChanges.Reverse()) Label(changes, "버전 " + change.revision + " · " + change.reason + "\n이전: " + string.Join(", ", change.before) + "\n변경: " + string.Join(", ", change.after));
        }
        private void RequireCurrent()
        {
            BrainWorkspace.Require(!EditorApplication.isCompiling && !EditorApplication.isUpdating && !EditorApplication.isPlayingOrWillChangePlaymode, "Unity가 처리 중입니다. 끝난 뒤 다시 시도하세요.");
            var current = new BrainTaskService(Root, json).Load();
            BrainWorkspace.Require(current != null && current.id == draft.taskId && current.revision == draft.revision, "작업이 바뀌었습니다. 입력은 보존했습니다. 최신 작업을 다시 읽으세요.");
        }
        private void SaveScope()
        {
            RequireCurrent();
            var paths = draft.paths.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).Where(p => p.Length > 0).ToArray();
            var closeReason = draft.closeReason;
            Lifecycle.SetScope(draft.taskId, draft.revision, paths, draft.scopeReason);
            active = new BrainTaskService(Root, json).Load(); BindActive();
            draft.closeReason = closeReason; draft.edited = closeReason.Length > 0; Persist(); CreateGUI();
            Show("범위를 저장했습니다. 처음 기준선과 문서 확인 상태는 유지됩니다.");
        }
        private void CloseTask(string disposition)
        {
            RequireCurrent();
            BrainWorkspace.Require(draft.paths == string.Join("\n", active.allowedPaths) && string.IsNullOrWhiteSpace(draft.scopeReason), "저장하지 않은 범위 입력이 있습니다. 먼저 저장하거나 입력을 버리세요.");
            var result = Lifecycle.Close(draft.taskId, draft.revision, disposition, draft.closeReason);
            historyId = result.task.id; draft = new Draft(); Persist(); CreateGUI();
            Show(disposition == "completed" ? "완료 기록을 보존하고 작업을 종료했습니다." : "미완료 기록을 보존하고 작업을 종료했습니다. 새 작업은 탐색 화면에서 시작하세요.");
        }
        private void BuildHistory(VisualElement parent)
        {
            Label(parent, "이전 작업 기록", true);
            var id = new TextField("작업 ID") { name = "history-id", value = historyId }; parent.Add(id);
            id.RegisterValueChangedCallback(e => historyId = e.newValue);
            var result = new VisualElement { name = "history-result" }; parent.Add(ActionButton("기록 조회", "read-history", () => RenderHistory(result, historyId.Trim()))); parent.Add(result);
            var directory = new BrainWorkspace(Root).Resolve(".projectbrain/tasks/archive");
            if (!Directory.Exists(directory)) Label(parent, "종료된 작업이 아직 없습니다.");
            else
            {
                var records = new System.Collections.Generic.List<BrainTaskHistoryView>();
                foreach (var file in Directory.GetFiles(directory, "*.json"))
                {
                    try { records.Add(Lifecycle.History(Path.GetFileNameWithoutExtension(file))); }
                    catch (Exception e) { Label(parent, "읽을 수 없는 기록 · " + Path.GetFileName(file) + " · " + e.Message); }
                }
                Label(parent, "최근 종료 " + Math.Min(20, records.Count) + " / 전체 " + records.Count + "건 · 나머지는 ID로 조회");
                foreach (var item in records.OrderByDescending(r => r.closedUtc, StringComparer.Ordinal).Take(20))
                    parent.Add(ActionButton((item.disposition == "completed" ? "완료 · " : "미완료 · ") + item.task.purpose, "history-" + item.task.id, () => { historyId = item.task.id; id.SetValueWithoutNotify(historyId); RenderHistory(result, historyId); }));
            }
            if (!string.IsNullOrEmpty(historyId)) Run(() => RenderHistory(result, historyId));
        }
        private void RenderHistory(VisualElement parent, string id)
        {
            parent.Clear(); var item = Lifecycle.History(id);
            Label(parent, item.task.purpose, true);
            Label(parent, (item.disposition == "active" ? "진행 중" : item.disposition == "completed" ? "완료 종료" : "미완료 종료") + " · " + item.closedUtc);
            Label(parent, "진행: " + item.task.progress + "\n결정: " + item.task.decisions + "\n미해결: " + item.task.unresolved + "\n다음 행동: " + item.task.nextAction);
            Label(parent, "종료 이유: " + item.reason + "\n보존한 기준 파일: " + item.task.baselineFileCount + "개");
            foreach (var change in item.scopeChanges) Label(parent, "범위 변경 · " + change.reason + "\n" + string.Join(", ", change.before) + " → " + string.Join(", ", change.after));
            foreach (var group in item.remainingReasons.GroupBy(r => r.code)) Label(parent, "종료 당시 " + group.Count() + "건 · " + group.First().nextAction);
            Label(parent, item.disposition == "active" ? "현재 완료 조건은 완료 조건 확인에서 조회하세요." : "종료 시점의 기록입니다. 현재 코드의 검증 결과와 구분합니다.");
        }
        private void Input(VisualElement parent, string label, string name, string value, Action<string> assign)
        {
            var field = new TextField(label) { name = name, value = value, multiline = true };
            BrainTheme.WrapField(field, 60);
            field.RegisterValueChangedCallback(e => { assign(e.newValue); draft.edited = true; Persist(); }); parent.Add(field);
        }
        private Button ActionButton(string text, string name, Action action)
        {
            var button = new Button(() => Run(action)) { text = text, name = name };
            if (name == "check-completion" || name == "save-scope") button.AddToClassList("primary");
            button.style.whiteSpace = WhiteSpace.Normal; button.style.height = StyleKeyword.Auto; button.style.minHeight = 26; return button;
        }
        private static void Label(VisualElement parent, string text, bool heading = false) => parent.Add(new Label(text) { style = { whiteSpace = WhiteSpace.Normal, marginTop = heading ? 12 : 4, marginBottom = 5, fontSize = heading ? 16 : 12 } });
        private void Show(string text, bool warning = false) { message.text = text; message.messageType = warning ? HelpBoxMessageType.Warning : HelpBoxMessageType.Info; }
        private void Run(Action action) { try { action(); } catch (Exception e) { message.text = e.Message; message.messageType = HelpBoxMessageType.Error; } }
    }
}
