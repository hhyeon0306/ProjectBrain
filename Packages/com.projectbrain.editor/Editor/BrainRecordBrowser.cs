using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace ProjectBrain
{
    // One owner for work and verification history; code entry points only apply a visible filter.
    internal sealed class BrainRecordBrowser
    {
        [Serializable] internal sealed class NumberEntry { public string id; public int number; }
        [Serializable] internal sealed class NumberIndex { public int schemaVersion = 1; public NumberEntry[] entries = Array.Empty<NumberEntry>(); }
        private readonly string root = ScriptDocumentService.ProjectRoot;
        private readonly UnityBrainJson json = new UnityBrainJson();
        private readonly List<BrainTaskHistoryView> tasks = new List<BrainTaskHistoryView>();
        private readonly List<BrainVerificationRecord> runs = new List<BrainVerificationRecord>();
        private readonly List<BrainEditReceipt> edits = new List<BrainEditReceipt>();
        private readonly List<string> issues = new List<string>();
        private Dictionary<string, int> numbers;
        private BrainGraphService graph;
        private ScrollView page;
        private string section, contextId, search = "", state = "전체";
        private int offset;
        private System.Action<string, string> navigate;

        internal static NumberIndex AppendNumbers(NumberIndex index, IEnumerable<string> ids)
        {
            BrainWorkspace.Require(index != null && index.schemaVersion == 1 && index.entries != null, "작업 번호 목록을 읽을 수 없습니다.");
            BrainWorkspace.Require(index.entries.All(e => e != null && Guid.TryParse(e.id, out _) && e.number > 0) && index.entries.Select(e => e.id).Distinct().Count() == index.entries.Length && index.entries.Select(e => e.number).Distinct().Count() == index.entries.Length, "작업 번호가 충돌했습니다. 원본을 보존합니다.");
            var list = index.entries.ToList(); int next = list.Count == 0 ? 1 : checked(list.Max(e => e.number) + 1);
            foreach (var id in ids.Distinct()) if (!list.Any(e => e.id == id)) list.Add(new NumberEntry { id = id, number = next++ });
            return new NumberIndex { entries = list.ToArray() };
        }
        private void Read()
        {
            graph = BrainPresentation.Graph();
            var lifecycle = new BrainTaskLifecycle(root, json, AssetDatabase.GUIDToAssetPath);
            var active = new BrainTaskService(root, json).Load();
            if (active != null) tasks.Add(lifecycle.History(active.id));
            var directory = Path.Combine(root, ".projectbrain/tasks/archive");
            if (Directory.Exists(directory)) foreach (var file in Directory.GetFiles(directory, "*.json"))
            {
                try { var item = lifecycle.History(Path.GetFileNameWithoutExtension(file)); if (tasks.All(t => t.task.id != item.task.id)) tasks.Add(item); }
                catch (Exception e) { issues.Add("작업 기록 · " + Path.GetFileName(file) + " · " + e.Message); }
            }
            var evidence = new BrainVerificationStore(root, json);
            directory = Path.Combine(root, ".projectbrain/evidence");
            if (Directory.Exists(directory)) foreach (var file in Directory.GetFiles(directory, "*.json"))
            {
                try { runs.Add(evidence.Load(Path.GetFileNameWithoutExtension(file))); }
                catch (Exception e) { issues.Add("검증 기록 · " + Path.GetFileName(file) + " · " + e.Message); }
            }
            directory = Path.Combine(root, ".projectbrain/edits");
            if (Directory.Exists(directory)) foreach (var file in Directory.GetFiles(directory, "*.json"))
            {
                try
                {
                    var receipt = json.Read<BrainEditReceipt>(File.ReadAllText(file));
                    BrainWorkspace.Require(receipt.schemaVersion == 1 && receipt.id == Path.GetFileNameWithoutExtension(file) && Guid.TryParse(receipt.taskId, out _) && (receipt.state == "applied" || receipt.state == "prepared"), "편집 기록 형식 오류");
                    edits.Add(receipt);
                }
                catch (Exception e) { issues.Add("편집 기록 · " + Path.GetFileName(file) + " · " + e.Message); }
            }
            // Stable display numbers are separate from immutable task/archive bytes.
            var indexPath = Path.Combine(root, ".projectbrain/tasks/display-index.json");
            using (new FileStream(Path.Combine(root, "Library/ProjectBrain.TaskNumbers.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                var previous = File.Exists(indexPath) ? json.Read<NumberIndex>(File.ReadAllText(indexPath)) : new NumberIndex();
                var next = AppendNumbers(previous, tasks.OrderBy(t => t.task.createdUtc, StringComparer.Ordinal).ThenBy(t => t.task.id, StringComparer.Ordinal).Select(t => t.task.id));
                if (json.Write(previous) != json.Write(next)) BrainStore.AtomicWrite(indexPath, json.Write(next));
                numbers = next.entries.ToDictionary(e => e.id, e => e.number);
            }
        }
        internal void Build(VisualElement rootElement, string selectedSection, string selectedContext, System.Action<string, string> change, System.Action settings)
        {
            section = selectedSection; contextId = selectedContext; navigate = change;
            page = BrainPresentation.Shell(rootElement, "작업·검증 기록", "어떤 일을 왜 했고, 무엇을 확인했는지 읽습니다.", out var nav, () => change(section, contextId));
            foreach (var item in new[] { ("overview", "전체 요약"), ("work", "작업 내역"), ("errors", "오류·미해결"), ("tests", "테스트·검증") })
                BrainPresentation.Nav(nav, item.Item2, section == item.Item1, () => change(item.Item1, contextId));
            nav.Add(BrainPresentation.Text("관리", "reader-caption")); BrainPresentation.Nav(nav, "작업 설정", false, settings);
            try
            {
                Read();
                nav.Add(BrainPresentation.Text("작업 " + tasks.Count + "개 · 검증 " + runs.Count + "회", "reader-subtitle"));
                if (!string.IsNullOrEmpty(contextId))
                {
                    string title = graph.Nodes.TryGetValue(contextId, out var node) ? node.title : contextId;
                    BrainPresentation.Card(nav, "현재 필터", title, "연결 대상 또는 작업 허용 범위에 포함된 기록입니다. 파일별 테스트 보장을 뜻하지 않습니다.");
                    BrainPresentation.Action(nav, "전체 기록 보기", null, () => change(section, ""));
                }
                if (issues.Count > 0)
                {
                    var warning = new Foldout { text = "읽지 못한 기록 " + issues.Count + "개", value = false }; nav.Add(warning);
                    foreach (var issue in issues) warning.Add(BrainPresentation.Text(issue));
                }
                Render();
            }
            catch (Exception e) { page.Add(new HelpBox("기록을 읽지 못했습니다. 원본은 유지됩니다. " + e.Message, HelpBoxMessageType.Error)); }
        }
        private bool Related(BrainTaskHistoryView item)
        {
            if (string.IsNullOrEmpty(contextId)) return true;
            if (!graph.Nodes.TryGetValue(contextId, out var node)) return item.task.targetNodeIds.Contains(contextId);
            var scope = BrainPresentation.Contents(graph, contextId);
            if (item.task.targetNodeIds.Any(id => scope.Contains(id) || graph.Nodes.ContainsKey(id) && BrainPresentation.Contents(graph, id).Contains(contextId))) return true;
            var code = BrainPresentation.CodeFor(graph, node);
            var path = code == null ? "" : AssetDatabase.GUIDToAssetPath(code.assetGuid);
            return path.Length > 0 && item.task.allowedPaths.Any(p => p.EndsWith("/", StringComparison.Ordinal) ? path.StartsWith(p, StringComparison.Ordinal) : path == p);
        }
        private string Number(BrainTaskHistoryView item) => "#" + numbers[item.task.id].ToString("000");
        private static string Status(BrainTaskHistoryView item) => item.disposition == "active" ? "진행 중" : item.disposition == "completed" ? "완료" : "미완료 종료";
        private void Controls()
        {
            var row = new VisualElement(); row.AddToClassList("reader-actions"); page.Add(row);
            var field = new TextField { value = search }; field.textEdition.placeholder = "제목·내용 검색"; field.style.flexGrow = 1; row.Add(field);
            var states = section == "work" ? new List<string> { "전체", "진행 중", "완료", "미완료 종료" } : new List<string> { "전체", "통과", "실패", "중단됨", "시간 초과", "기준 변경", "검증 중" };
            if (!states.Contains(state)) state = "전체";
            var filter = new DropdownField(states, states.IndexOf(state)); row.Add(filter);
            // Update list only; keep input focus while typing.
            var list = new VisualElement(); page.Add(list);
            System.Action redraw = () => { list.Clear(); RenderList(list); };
            field.RegisterValueChangedCallback(e => { search = e.newValue; offset = 0; redraw(); });
            filter.RegisterValueChangedCallback(e => { state = e.newValue; offset = 0; redraw(); }); redraw();
        }
        private void Render()
        {
            page.Clear();
            var relevant = tasks.Where(Related).ToArray(); var ids = new HashSet<string>(relevant.Select(t => t.task.id));
            var records = runs.Where(r => string.IsNullOrEmpty(contextId) || ids.Contains(r.taskId)).OrderByDescending(r => r.startedUtc, StringComparer.Ordinal).ToArray();
            if (section == "overview")
            {
                BrainPresentation.Hero(page, "WORKSPACE OVERVIEW", "지금 확인할 내용", "작업의 결과와 남은 판단을 먼저 확인하세요.");
                var metrics = new VisualElement(); metrics.AddToClassList("reader-metrics"); page.Add(metrics);
                BrainPresentation.Card(metrics, "진행 중", relevant.Count(t => t.disposition == "active").ToString(), "현재 작업");
                BrainPresentation.Card(metrics, "검증 기록", records.Length.ToString(), "실제 실행 횟수");
                BrainPresentation.Card(metrics, "미해결 기록", relevant.Count(t => !string.IsNullOrWhiteSpace(t.task.unresolved)).ToString(), "확인할 내용이 있는 작업");
                foreach (var item in relevant.OrderBy(t => t.disposition == "active" ? 0 : 1).ThenByDescending(t => t.task.updatedUtc).Take(3))
                    BrainPresentation.Action(page, Number(item) + " · " + item.task.purpose, Status(item) + " · " + BrainPresentation.Summary(item.task.progress), () => TaskDetail(item));
                if (records.Length > 0) RunCard(page, records[0]);
                if (relevant.Length == 0) BrainPresentation.Card(page, "EMPTY", "연결된 작업이 없습니다", "전체 기록으로 전환하거나 기록의 대상 연결을 확인하세요.");
                return;
            }
            BrainPresentation.Hero(page, section == "work" ? "WORK LOG" : section == "tests" ? "VERIFICATION" : "ISSUES", section == "work" ? "작업 내역" : section == "tests" ? "테스트·검증 내역" : "오류와 미해결 사항", section == "tests" ? "실행 시점의 결과입니다. 현재 코드에 대한 유효성은 결과 상세에서 별도로 확인합니다." : section == "errors" ? "저장된 미해결 메모와 실패·중단 기록입니다. 모든 런타임 오류를 자동 수집한 목록은 아닙니다." : "번호는 작업에 고정됩니다. 상세에서 배경·판단·결과와 연결 근거를 확인하세요.");
            if (section == "errors") foreach (var item in relevant.Where(t => !string.IsNullOrWhiteSpace(t.task.unresolved)).Take(10))
                BrainPresentation.Action(page, Number(item) + " · " + item.task.purpose, BrainPresentation.Summary(item.task.unresolved), () => TaskDetail(item));
            Controls();
        }
        private string RunState(BrainVerificationRecord run) => run.state == "stale" ? "기준 변경" : BrainTheme.VerificationName(run.state);
        private void RenderList(VisualElement list)
        {
            var relevant = tasks.Where(Related).ToArray();
            if (section == "work")
            {
                var filtered = relevant.Where(t => (state == "전체" || Status(t) == state) && Match(t.task.purpose + " " + t.task.progress + " " + t.task.decisions)).OrderByDescending(t => t.task.createdUtc).ToArray();
                foreach (var item in filtered.Skip(offset).Take(20)) BrainPresentation.Action(list, Number(item) + " · " + item.task.purpose, Status(item) + " · " + BrainPresentation.Date(item.task.updatedUtc) + "\n" + BrainPresentation.Summary(item.task.progress), () => TaskDetail(item));
                Paging(list, filtered.Length);
            }
            else
            {
                var ids = new HashSet<string>(relevant.Select(t => t.task.id));
                var filtered = runs.Where(r => (string.IsNullOrEmpty(contextId) || ids.Contains(r.taskId)) && (section != "errors" || r.state == "failed" || r.state == "interrupted" || r.state == "timeout") && (state == "전체" || RunState(r) == state) && Match(r.kind + " " + r.summary + " " + RunState(r))).OrderByDescending(r => r.startedUtc).ToArray();
                foreach (var run in filtered.Skip(offset).Take(20)) RunCard(list, run);
                Paging(list, filtered.Length);
            }
        }
        private bool Match(string text) => string.IsNullOrWhiteSpace(search) || text.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        private void Paging(VisualElement parent, int count)
        {
            if (count == 0) { BrainPresentation.Card(parent, "EMPTY", "표시할 기록이 없습니다", "기록 없음은 오류 없음이나 테스트 통과를 의미하지 않습니다."); return; }
            var row = new VisualElement(); row.AddToClassList("reader-actions"); parent.Add(row);
            var previous = BrainTheme.Button("이전", () => { offset = Math.Max(0, offset - 20); Render(); }); previous.SetEnabled(offset > 0); row.Add(previous);
            row.Add(BrainPresentation.Text((offset + 1) + "–" + Math.Min(offset + 20, count) + " / " + count, "reader-subtitle"));
            var next = BrainTheme.Button("다음", () => { offset += 20; Render(); }); next.SetEnabled(offset + 20 < count); row.Add(next);
        }
        private void Back() { page.Clear(); page.Add(BrainTheme.Button("← 목록으로", Render)); page.scrollOffset = UnityEngine.Vector2.zero; }
        private void TaskDetail(BrainTaskHistoryView item)
        {
            Back();
            BrainPresentation.Hero(page, Number(item) + " · " + Status(item), item.task.purpose, "시작 " + BrainPresentation.Date(item.task.createdUtc) + " · 갱신 " + BrainPresentation.Date(item.task.updatedUtc));
            BrainPresentation.Fact(page, "01 / PURPOSE", "작업 배경과 목적", item.task.purpose);
            BrainPresentation.Fact(page, "02 / DECISION", "판단과 결정", string.IsNullOrWhiteSpace(item.task.decisions) ? "판단 근거가 아직 기록되지 않았습니다." : item.task.decisions);
            BrainPresentation.Fact(page, "03 / RESULT", "작업 내용과 진행 결과", string.IsNullOrWhiteSpace(item.task.progress) ? "진행 내용이 아직 기록되지 않았습니다." : item.task.progress);
            BrainPresentation.Fact(page, "04 / FOLLOW UP", "남은 확인과 다음 행동", (string.IsNullOrWhiteSpace(item.task.unresolved) ? "미해결 내용 미기록" : item.task.unresolved) + "\n\n" + item.task.nextAction);
            if (!string.IsNullOrWhiteSpace(item.reason)) BrainPresentation.Card(page, "CLOSURE", "종료 기록", item.reason);
            var editRecords = edits.Where(e => e.taskId == item.task.id).OrderByDescending(e => e.createdUtc).ToArray();
            var editCard = BrainPresentation.Card(page, "CHANGE HISTORY", "기록된 편집 · " + editRecords.Length + "건", "Brain 편집 도구를 통한 기록입니다. 일반 파일 편집의 모든 변경 과정을 포함하지는 않습니다.");
            foreach (var receipt in editRecords.Take(20))
            {
                string title = graph.Nodes.TryGetValue(receipt.nodeId, out var edited) ? edited.title : "현재 구조도에서 제거된 항목";
                var fold = new Foldout { text = BrainPresentation.Date(receipt.createdUtc) + " · " + title + (receipt.state == "applied" ? " · 적용 기록" : " · 완료 확인 필요"), value = false }; editCard.Add(fold);
                fold.Add(BrainPresentation.Text("작업 종류 " + receipt.operation + "\n기록 주체 " + receipt.actor + "\n변경 전 " + receipt.beforeHash + "\n변경 후 " + receipt.afterHash));
            }
            var changesCard = new Foldout { text = "변경 파일 확인", value = false }; page.Add(changesCard);
            bool changesRead = false;
            changesCard.RegisterValueChangedCallback(e =>
            {
                if (!e.newValue || changesRead) return; changesRead = true;
                try
                {
                    if (item.disposition == "active") BrainWorkspace.Require(new BrainTaskService(root, json).Load()?.id == item.task.id, "활성 작업이 바뀌었습니다. 목록을 새로 읽으세요.");
                    var changes = item.disposition == "active" ? new BrainTaskService(root, json).Status().changes : json.Read<BrainTaskArchive>(File.ReadAllText(Path.Combine(root, item.archivePath))).changes;
                    changesCard.Add(BrainPresentation.Text(item.disposition == "active" ? "조회 시점의 작업 시작 기준 대비 변경입니다." : "작업 종료 시점에 보존한 변경입니다."));
                    foreach (var change in changes)
                    {
                        string label = change.kind == "added" ? "추가" : change.kind == "deleted" ? "삭제" : "수정";
                        changesCard.Add(BrainPresentation.Text(label + " · " + change.path + (change.allowed ? "" : " · 허용 범위 밖"), "reader-subtitle"));
                    }
                }
                catch (Exception error) { changesCard.Add(BrainPresentation.Text(error.Message)); }
            });
            var links = BrainPresentation.Card(page, "CONTEXT", "관련 설계와 코드");
            BrainPresentation.NodeLinks(links, graph, item.task.targetNodeIds.Concat(item.task.references ?? Array.Empty<string>()), id => BrainDocumentWindow.OpenNode(id));
            var records = runs.Where(r => r.taskId == item.task.id).OrderByDescending(r => r.startedUtc).ToArray();
            var evidence = BrainPresentation.Card(page, "EVIDENCE", "작업 범위의 검증 · " + records.Length + "회");
            foreach (var record in records.Take(10)) RunCard(evidence, record);
            if (records.Length > 10) evidence.Add(BrainPresentation.Text("나머지 실행은 테스트·검증 목록에서 확인하세요."));
            var meta = new Foldout { text = "작업 설정·원본 식별 정보", value = false }; page.Add(meta);
            meta.Add(BrainPresentation.Text("ID " + item.task.id + "\n버전 " + item.task.revision));
            meta.Add(BrainPresentation.Text("허용 경로\n" + string.Join("\n", item.task.allowedPaths)));
            foreach (var c in item.scopeChanges) meta.Add(BrainPresentation.Text(BrainPresentation.Date(c.changedUtc) + " · " + c.reason));
        }
        private void RunCard(VisualElement parent, BrainVerificationRecord run)
        {
            BrainPresentation.Action(parent, (run.kind == "compile" ? "컴파일" : "EditMode 테스트") + " · " + RunState(run), BrainPresentation.Date(run.startedUtc) + (run.kind == "editmode" ? " · 통과 " + run.passed + " / 전체 " + run.total : "") + "\n실행 당시의 결과", () => RunDetail(run));
        }
        private void RunDetail(BrainVerificationRecord run)
        {
            Back(); BrainPresentation.Hero(page, "VERIFICATION REPORT", (run.kind == "compile" ? "컴파일" : "EditMode 테스트") + " · " + RunState(run), "실행 " + BrainPresentation.Date(run.startedUtc));
            var report = new VisualElement(); report.AddToClassList("verification-report"); page.Add(report);
            if (run.kind == "editmode") report.Add(new BrainVerificationChart(run));
            var metrics = new VisualElement(); metrics.AddToClassList("reader-metrics"); metrics.style.flexGrow = 1; report.Add(metrics);
            var pass = BrainPresentation.Card(metrics, run.kind == "editmode" ? "통과" : "확인 어셈블리", run.passed.ToString()); pass.AddToClassList("metric-pass");
            var fail = BrainPresentation.Card(metrics, run.kind == "editmode" ? "실패" : "오류", run.failed.ToString()); if (run.failed > 0) fail.AddToClassList("metric-fail");
            BrainPresentation.Card(metrics, "건너뜀", run.skipped.ToString());
            var store = new BrainVerificationStore(root, json);
            try { BrainPresentation.Card(page, "CURRENT BASIS · 조회 " + DateTime.Now.ToString("HH:mm:ss"), "현재 코드와의 관계", store.CurrentPass(run, store.Snapshot()) ? "조회 시점의 스냅샷과 일치하는 통과 기록입니다. 문서 검토와 작업 완료는 별도입니다." : "조회 시점의 코드에 대한 통과 근거로 사용할 수 없습니다. 실행 당시 결과는 보존됩니다."); }
            catch (Exception e) { BrainPresentation.Card(page, "확인 필요", "현재 유효성을 확인하지 못했습니다", e.Message); }
            BrainPresentation.Card(page, "RESULT", "실행 요약", run.summary);
            var task = tasks.FirstOrDefault(t => t.task.id == run.taskId);
            if (task != null) BrainPresentation.Action(page, Number(task) + " · " + task.task.purpose, "이 검증이 속한 작업", () => TaskDetail(task));
            page.Add(BrainPresentation.Text("현재 저장된 결과는 실행 단위 집계입니다. 개별 테스트 케이스의 상세 로그·파일별 커버리지는 포함하지 않습니다.", "reader-subtitle"));
            var raw = new Foldout { text = "원본 기록과 실행 기준", value = false }; page.Add(raw);
            raw.Add(BrainPresentation.Text("실행 ID " + run.id + "\n작업 ID " + run.taskId + "\n스냅샷 " + run.snapshotHash + "\n종료 " + BrainPresentation.Date(run.endedUtc)));
        }
    }
}
