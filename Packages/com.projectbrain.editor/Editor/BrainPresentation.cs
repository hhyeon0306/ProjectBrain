using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectBrain
{
    // Presentation reads existing records. It never infers approval, coverage or completion.
    internal static class BrainPresentation
    {
        internal static BrainGraphService Graph() => new BrainGraphService(new BrainStore(Path.Combine(ScriptDocumentService.ProjectRoot, ".projectbrain"), new UnityBrainJson()));
        internal static Label Text(string value, string css = "reader-copy") => BrainTheme.Label(value ?? "", css);
        internal static VisualElement Card(VisualElement parent, string caption, string title, string body = null)
        {
            var card = new VisualElement(); card.AddToClassList("reader-card"); parent.Add(card);
            if (!string.IsNullOrEmpty(caption)) card.Add(Text(caption, "reader-caption"));
            if (!string.IsNullOrEmpty(title)) card.Add(Text(title, "reader-heading"));
            if (!string.IsNullOrWhiteSpace(body)) card.Add(Text(body));
            return card;
        }
        internal static void Action(VisualElement parent, string title, string description, System.Action action, bool primary = false)
        {
            var button = new Button(action); button.AddToClassList("reader-link");
            if (primary) button.AddToClassList("reader-link-primary");
            button.Add(Text(title + "  →", "reader-link-title"));
            if (!string.IsNullOrEmpty(description)) button.Add(Text(description, "reader-link-description"));
            parent.Add(button);
        }
        internal static ScrollView Shell(VisualElement root, string title, string subtitle, out VisualElement navigation, System.Action refresh)
        {
            root.Clear(); BrainTheme.Apply(root); root.AddToClassList("reader-window");
            root.style.paddingLeft = root.style.paddingRight = root.style.paddingTop = root.style.paddingBottom = 0;
            var header = new VisualElement(); header.AddToClassList("reader-header"); root.Add(header);
            var names = new VisualElement(); names.style.flexGrow = 1; names.style.minWidth = 0; header.Add(names);
            names.Add(Text("PROJECT BRAIN", "reader-caption")); names.Add(Text(title, "reader-title")); names.Add(Text(subtitle, "reader-subtitle"));
            header.Add(BrainTheme.Button("새로 읽기", refresh));
            var body = new VisualElement(); body.AddToClassList("reader-body"); root.Add(body);
            var nav = new ScrollView(); nav.AddToClassList("reader-navigation"); body.Add(nav); navigation = nav;
            var page = new ScrollView(); page.AddToClassList("reader-page"); body.Add(page); return page;
        }
        internal static void Nav(VisualElement parent, string title, bool selected, System.Action action)
        {
            var button = BrainTheme.Button(title, action); button.AddToClassList("reader-nav-item"); button.EnableInClassList("active", selected); parent.Add(button);
        }
        internal static void Hero(VisualElement parent, string caption, string title, string description)
        {
            var hero = Card(parent, caption, title, description); hero.AddToClassList("reader-hero");
        }
        internal static string Date(string value) => DateTimeOffset.TryParse(value, out var date) ? date.ToLocalTime().ToString("yyyy.MM.dd  HH:mm") : "시각 미기록";
        internal static string Summary(string value, int max = 150)
        {
            if (string.IsNullOrWhiteSpace(value)) return "설명이 아직 등록되지 않았습니다.";
            value = value.Replace('\r', ' ').Replace('\n', ' ').Trim();
            return value.Length <= max ? value : value.Substring(0, max) + "…";
        }
        internal static VisualElement Fact(VisualElement parent, string caption, string title, string content)
        {
            var card = Card(parent, caption, title, Summary(content, 220));
            if (!string.IsNullOrEmpty(content) && content.Length > 220)
            {
                var more = new Foldout { text = "전체 기록 읽기", value = false }; card.Add(more); more.Add(Text(content));
            }
            return card;
        }
        internal static HashSet<string> Contents(BrainGraphService graph, string id)
        {
            var result = new HashSet<string>(StringComparer.Ordinal); var queue = new Queue<string>(); queue.Enqueue(id);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue(); if (!result.Add(current)) continue;
                foreach (var r in graph.Relations.Where(r => r.from == current && (r.type == "contains" || r.type == "implemented_by" || r.type == "documented_by"))) queue.Enqueue(r.to);
            }
            return result;
        }
        internal static BrainNode CodeFor(BrainGraphService graph, BrainNode node)
        {
            if (node.type == "Code") return node;
            return graph.Relations.Where(r => r.to == node.id && r.type == "documented_by").Select(r => graph.Get(r.from)).FirstOrDefault(n => n.type == "Code");
        }
        internal static void NodeLinks(VisualElement parent, BrainGraphService graph, IEnumerable<string> ids, System.Action<string> open, int limit = 12)
        {
            var nodes = ids.Distinct().Where(graph.Nodes.ContainsKey).Select(graph.Get).OrderBy(n => n.title, StringComparer.Ordinal).ToArray();
            foreach (var n in nodes.Take(limit)) Action(parent, n.title, Summary(n.summary, 90), () => open(n.id));
            if (nodes.Length > limit)
            {
                var more = new Foldout { text = "나머지 " + (nodes.Length - limit) + "개 보기", value = false }; parent.Add(more);
                bool built = false; more.RegisterValueChangedCallback(e => { if (!e.newValue || built) return; built = true; NodeLinks(more, graph, nodes.Skip(limit).Select(n => n.id), open, limit); });
            }
            if (nodes.Length == 0) parent.Add(Text("연결된 항목이 없습니다.", "reader-subtitle"));
        }
        internal static void Document(VisualElement page, BrainGraphService graph, string id, System.Action<string> open, System.Action<MonoScript> edit, bool dependencies = false, System.Action<bool> switchTab = null)
        {
            var node = graph.Get(id); var code = CodeFor(graph, node);
            if (node.type == "Code")
            {
                var link = graph.Relations.FirstOrDefault(r => r.from == id && r.type == "documented_by");
                if (link != null) node = graph.Get(link.to);
            }
            var ancestor = graph.Parent(id);
            string caption = ancestor == null ? BrainTheme.TypeName(node.type) : graph.Get(ancestor).title + "  /  " + BrainTheme.TypeName(node.type);
            Hero(page, caption, code == null ? node.title : Path.GetFileNameWithoutExtension(code.title) + " 설계", node.summary);
            if (code != null)
            {
                var actions = new VisualElement(); actions.AddToClassList("reader-actions"); page.Add(actions);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(code.assetGuid));
                var editor = BrainTheme.Button("문서 편집", () => edit(script)); editor.SetEnabled(script != null); actions.Add(editor);
                var source = BrainTheme.Button("코드 열기", () => AssetDatabase.OpenAsset(script)); source.SetEnabled(script != null); actions.Add(source);
                actions.Add(BrainTheme.Button("관련 작업·검증 기록 →", () => BrainTaskWindow.OpenRecords(code.id, "work")));
                if (script == null) Card(page, "연결 확인 필요", "코드 파일을 찾을 수 없습니다", "저장된 문서를 표시하고 있습니다. 코드 연결을 복구한 뒤 편집할 수 있습니다.");
            }
            var tabs = new VisualElement(); tabs.AddToClassList("reader-actions"); page.Add(tabs);
            if (code != null)
            {
                Nav(tabs, "설계 설명", !dependencies, () => switchTab?.Invoke(false));
                Nav(tabs, "의존 관계", dependencies, () => switchTab?.Invoke(true));
            }
            if (dependencies && code != null)
            {
                var outgoing = graph.Relations.Where(r => r.type == "depends_on" && r.from == code.id).ToArray();
                var incoming = graph.Relations.Where(r => r.type == "depends_on" && r.to == code.id).ToArray();
                page.Add(Text("등록된 코드 관계입니다. 현재 자동 코드 분석 결과는 아닙니다.", "reader-subtitle"));
                var uses = Card(page, "OUTGOING", "이 코드가 사용하는 코드 · " + outgoing.Length);
                NodeLinks(uses, graph, outgoing.Select(r => r.to), open);
                var used = Card(page, "INCOMING", "이 코드를 사용하는 코드 · " + incoming.Length);
                NodeLinks(used, graph, incoming.Select(r => r.from), open);
                var provenance = new Foldout { text = "연결 근거와 출처", value = false }; page.Add(provenance);
                foreach (var r in outgoing.Concat(incoming)) provenance.Add(Text(graph.Get(r.from).title + " → " + graph.Get(r.to).title + " · " + r.source));
                return;
            }
            if (node.type == "Project" || node.type == "Domain" || node.type == "Feature")
            {
                var children = graph.Children(node.id);
                var structure = Card(page, "01 / STRUCTURE", node.type == "Project" ? "아키텍처 구성" : "구성 기능", "등록된 계층과 구현 연결을 기준으로 표시합니다.");
                structure.Add(new BrainArchitectureView(graph, node.id, open));
                var ids = Contents(graph, node.id);
                if (!string.IsNullOrWhiteSpace(node.body)) Markdown(page, node.body);
                var implementation = Card(page, "IMPLEMENTATION", "관련 구현 코드 · " + ids.Count(x => graph.Get(x).type == "Code"));
                if (node.type == "Project")
                {
                    var fold = new Foldout { text = "전체 구현 코드 펼치기", value = false }; implementation.Add(fold);
                    bool built = false; fold.RegisterValueChangedCallback(e => { if (!e.newValue || built) return; built = true; NodeLinks(fold, graph, ids.Where(x => graph.Get(x).type == "Code"), open); });
                }
                else NodeLinks(implementation, graph, ids.Where(x => graph.Get(x).type == "Code"), open);
            }
            else if (node.type == "Document")
            {
                Markdown(page, node.body, node.summary);
                var imageIds = graph.Relations.Where(r => r.from == node.id && r.type == "illustrated_by").Select(r => r.to).ToArray();
                if (imageIds.Length > 0)
                {
                    var media = Card(page, "RESOURCES", "첨부 자료");
                    foreach (var imageId in imageIds)
                    {
                        var n = graph.Get(imageId); var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(n.assetGuid));
                        if (texture != null) media.Add(new Image { image = texture, scaleMode = ScaleMode.ScaleToFit, style = { height = 240 } });
                        media.Add(Text(n.title, "reader-subtitle"));
                    }
                }
                Review(page, graph, node.id);
            }
            else Card(page, "DOCUMENT", "설계 문서가 아직 연결되지 않았습니다", "문서 편집에서 역할과 설계 의도를 작성할 수 있습니다.");
            var metadata = new Foldout { text = "자료 정보 · ID와 저장 기준", value = false }; page.Add(metadata);
            metadata.Add(Text(node.id)); metadata.Add(Text("갱신 " + Date(node.updatedUtc)));
            if (code != null) metadata.Add(Text(code.lastKnownPath));
        }
        internal static void Markdown(VisualElement page, string body, string lead = null)
        {
            if (string.IsNullOrWhiteSpace(body)) { Card(page, "DESIGN", "상세 설명이 아직 없습니다"); return; }
            var parts = body.Replace("\r", "").Split(new[] { "## " }, StringSplitOptions.RemoveEmptyEntries); int number = 0;
            foreach (var part in parts)
            {
                var split = part.IndexOf('\n'); var title = split < 0 ? "설계 설명" : part.Substring(0, split).Trim();
                var content = split < 0 ? part : part.Substring(split + 1).Trim();
                if (title == "역할" && content == lead?.Trim()) continue;
                if (title.Contains("해시"))
                {
                    var fold = new Foldout { text = "저장 당시 코드 기준", value = false }; fold.Add(Text(content)); page.Add(fold); continue;
                }
                Card(page, (++number).ToString("00") + " / DESIGN", title, content);
            }
        }
        internal static void Review(VisualElement page, BrainGraphService graph, string documentId)
        {
            var json = new UnityBrainJson(); var root = ScriptDocumentService.ProjectRoot;
            var service = new BrainCompletionService(root, json, AssetDatabase.GUIDToAssetPath);
            var status = service.InspectDocument(documentId, json.Write(graph.Get(documentId)));
            var card = Card(page, "REVIEW", "문서 검토 · " + BrainTheme.ReviewName(status.state), "저장된 설명과 연결 코드를 확인한 사람이 검토를 기록합니다. 테스트 결과와는 별개입니다.");
            var fold = new Foldout { text = "검토할 코드와 확인 절차", value = false }; card.Add(fold);
            foreach (var path in status.codePaths) fold.Add(Text(path, "reader-subtitle"));
            var active = new BrainTaskService(root, json).Load();
            if (active == null) { fold.Add(Text("진행 중인 작업이 있을 때 확인을 기록할 수 있습니다.")); return; }
            var ack = new Toggle("현재 문서와 연결 코드를 읽고 내용이 맞는지 확인했습니다."); ack.AddToClassList("review-acknowledgement"); fold.Add(ack);
            var result = Text("");
            var save = BrainTheme.Button("사람 확인 기록", () =>
            {
                try { service.ConfirmFromHuman(active.id, active.revision, documentId, status.snapshotHash); result.text = "확인을 기록했습니다. 작업 완료와는 별개입니다."; ack.SetValueWithoutNotify(false); }
                catch (Exception e) { result.text = e.Message; }
            });
            save.SetEnabled(false); ack.RegisterValueChangedCallback(e => save.SetEnabled(e.newValue && status.state != "missing-code"));
            save.clicked += () => save.SetEnabled(false); fold.Add(save); fold.Add(result);
        }
    }
}
