using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectBrain
{
    // UI-only coordinates. Stored graph IDs/edges remain untouched by layout, drag and filters.
    public sealed class BrainMapView : VisualElement
    {
        private readonly BrainGraphService graph;
        private readonly Action<string> select;
        private readonly Dictionary<string, Vector2> positions = new Dictionary<string, Vector2>();
        private readonly Dictionary<string, Button> labels = new Dictionary<string, Button>();
        private HashSet<string> visibleNodes = new HashSet<string>();
        private readonly HashSet<string> hiddenTypes = new HashSet<string>();
        private readonly HashSet<string> curvedRelations = new HashSet<string>();
        private string selected, query = "";
        private bool local;
        private HashSet<string> scopeIds;
        public Action<string> Activated;
        private Vector2 offset, lastPointer;
        private float zoom = 1;
        private int pointer = -1;
        private string dragged;
        private bool moved;
        private int pressClicks;
        public Action Changed;
        public int VisibleCount => visibleNodes.Count;
        public int HiddenLabelCount { get; private set; }
        public float Zoom => zoom;
        public string Selected => selected;
        public IReadOnlyDictionary<string, Vector2> Positions => positions;
        public string[] VisibleIds => visibleNodes.OrderBy(id => id, StringComparer.Ordinal).ToArray();

        public BrainMapView(BrainGraphService graph, Action<string> select)
        {
            this.graph = graph; this.select = select;
            var directions = new HashSet<(string, string)>(graph.Relations.Select(r => (r.from, r.to)));
            foreach (var relation in graph.Relations)
                if (relation.from != relation.to && directions.Contains((relation.to, relation.from))) curvedRelations.Add(relation.id);
            name = "brain-map"; style.flexGrow = 1; style.overflow = Overflow.Hidden; focusable = true;
            ComputeLayout();
            foreach (var node in graph.Nodes.Values.OrderBy(n => n.id, StringComparer.Ordinal))
            {
                var id = node.id;
                var title = node.type == "Evidence" ? "검증 기록 · " + id.Substring(Math.Max(0, id.Length - 6)) : node.title;
                var button = new Button(() => { if (!moved) select(id); }) { text = title, name = "map-node-" + id, tooltip = BrainTheme.TypeName(node.type) + " · " + node.title + "\n" + node.summary + "\n" + id };
                button.AddToClassList("node-label");
                if (node.type == "Project") button.AddToClassList("node-root");
                else if (node.type == "Domain") button.AddToClassList("node-domain");
                labels.Add(id, button); Add(button);
                button.RegisterCallback<ClickEvent>(e => { if (e.button == 0 && e.clickCount == 2 && !moved) { Activated?.Invoke(id); e.StopPropagation(); } });
                button.AddManipulator(new ContextualMenuManipulator(e =>
                {
                    e.menu.AppendAction(node.type == "Code" ? "문서 열기" : "선택 항목 열기", _ => Activated?.Invoke(id));
                    if (node.type == "Code") e.menu.AppendAction("코드 열기", _ =>
                    {
                        var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.MonoScript>(UnityEditor.AssetDatabase.GUIDToAssetPath(node.assetGuid));
                        if (asset != null) UnityEditor.AssetDatabase.OpenAsset(asset);
                    });
                }));
            }
            generateVisualContent += Draw;
            RegisterCallback<GeometryChangedEvent>(e => { if (e.newRect.size != e.oldRect.size) Fit(); });
            RegisterCallback<WheelEvent>(e => { ZoomAt(Mathf.Pow(1.05f, -e.delta.y), e.localMousePosition); e.StopPropagation(); });
            RegisterCallback<PointerDownEvent>(Down, TrickleDown.TrickleDown);
            RegisterCallback<PointerMoveEvent>(Move);
            RegisterCallback<PointerUpEvent>(Up);
            RegisterCallback<PointerCaptureOutEvent>(_ => { pointer = -1; dragged = null; });
            RegisterCallback<KeyDownEvent>(e => { if (e.keyCode == KeyCode.F) { Fit(); e.StopPropagation(); } });
            RefreshVisible();
        }
        public void SetSelected(string id) { selected = id; RefreshVisible(); if (local) Fit(); }
        public void SetScope(HashSet<string> ids) { scopeIds = ids; RefreshVisible(); Fit(); }
        public void SetQuery(string text) { query = text ?? ""; RefreshVisible(); Fit(); }
        public void SetLocal(bool value) { local = value; RefreshVisible(); Fit(); }
        public void ShowType(string type, bool show) { if (show) hiddenTypes.Remove(type); else hiddenTypes.Add(type); RefreshVisible(); Fit(); }
        public void ZoomBy(float factor) => ZoomAt(factor, new Vector2(contentRect.width * .4f, contentRect.height * .5f));
        private void ZoomAt(float factor, Vector2 anchor)
        {
            if (float.IsNaN(anchor.x) || float.IsNaN(anchor.y)) return;
            var next = Mathf.Clamp(zoom * factor, .2f, 2.5f);
            offset = anchor - (anchor - offset) * (next / zoom); zoom = next; UpdatePositions();
        }
        public void Fit()
        {
            if (visibleNodes.Count == 0 || float.IsNaN(contentRect.width) || float.IsNaN(contentRect.height) || contentRect.width < 10 || contentRect.height < 10) { UpdatePositions(); return; }
            var points = visibleNodes.Select(id => positions[id]).ToArray();
            var min = new Vector2(points.Min(p => p.x), points.Min(p => p.y));
            var max = new Vector2(points.Max(p => p.x), points.Max(p => p.y));
            // Use the entire map; screen-space separation avoids the floating inspector below.
            var size = new Vector2(Mathf.Max(160, contentRect.width - 240), Mathf.Max(100, contentRect.height - 140));
            zoom = Mathf.Clamp(Mathf.Min(size.x / Mathf.Max(1, max.x - min.x), size.y / Mathf.Max(1, max.y - min.y)), .2f, 1.15f);
            offset = new Vector2(48, 68) + size * .5f - (min + max) * .5f * zoom;
            SeparateLabels();
            UpdatePositions();
        }
        private void SeparateLabels()
        {
            // Fit uses fixed-size readable labels. Resolve their screen-space collisions after scaling.
            var ids = visibleNodes.OrderBy(id => id, StringComparer.Ordinal).ToArray();
            var points = ids.Select(Screen).ToArray();
            float right = Mathf.Max(190, contentRect.width - 190), bottom = Mathf.Max(220, contentRect.height - 100);
            for (int pass = 0; pass < 180; pass++)
            {
                bool overlap = false;
                for (int i = 0; i < ids.Length; i++) for (int j = i + 1; j < ids.Length; j++)
                {
                    var d = points[j] - points[i];
                    float x = 182 - Mathf.Abs(d.x), y = 40 - Mathf.Abs(d.y);
                    if (x <= 0 || y <= 0) continue;
                    overlap = true;
                    var shift = x < y ? new Vector2((d.x >= 0 ? 1 : -1) * (x * .51f + 1), 0) : new Vector2(0, (d.y >= 0 ? 1 : -1) * (y * .51f + 1));
                    points[i] -= shift; points[j] += shift;
                }
                for (int i = 0; i < points.Length; i++)
                {
                    var point = new Vector2(Mathf.Clamp(points[i].x, 34, right), Mathf.Clamp(points[i].y, 88, bottom));
                    points[i] = point;
                }
                if (!overlap) break;
            }
            for (int i = 0; i < ids.Length; i++) positions[ids[i]] = (points[i] - offset) / zoom;
        }
        private void RefreshVisible()
        {
            var scope = new HashSet<string>(graph.Nodes.Keys);
            if (local && selected != null)
            {
                scope = new HashSet<string> { selected };
                for (int depth = 0; depth < 2; depth++)
                    foreach (var id in scope.ToArray()) foreach (var r in graph.Around(id)) { scope.Add(r.from); scope.Add(r.to); }
            }
            if (scopeIds != null) scope.IntersectWith(scopeIds);
            visibleNodes = new HashSet<string>(scope.Where(id => !hiddenTypes.Contains(graph.Get(id).type) &&
                (query.Length == 0 || (graph.Get(id).title + " " + graph.Get(id).summary + " " + id).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)));
            foreach (var pair in labels)
            {
                pair.Value.style.display = visibleNodes.Contains(pair.Key) ? DisplayStyle.Flex : DisplayStyle.None;
                pair.Value.EnableInClassList("selected", pair.Key == selected);
            }
            UpdatePositions();
        }
        private void UpdatePositions()
        {
            // Preserve every node/edge while keeping names legible at overview scale.
            // A selected node always gets its name; glyphs remain clickable when names are omitted.
            var occupied = new List<Rect>();
            HiddenLabelCount = 0;
            foreach (var id in visibleNodes.OrderBy(id => id == selected ? 0 : graph.Get(id).type == "Project" ? 1 : graph.Get(id).type == "Domain" ? 2 : graph.Get(id).type == "Evidence" ? 4 : 3).ThenBy(id => id, StringComparer.Ordinal))
            {
                var p = Screen(id); var label = labels[id];
                bool major = graph.Get(id).type == "Project" || graph.Get(id).type == "Domain";
                float labelOffset = major ? NodeRadius(id) + 8 : 12;
                label.style.left = p.x + labelOffset; label.style.top = p.y - 14;
                float measured = label.MeasureTextSize(label.text, 0, MeasureMode.Undefined, 0, MeasureMode.Undefined).x;
                float maxWidth = major ? 200 : 164;
                float width = float.IsNaN(measured) ? maxWidth : Mathf.Min(maxWidth, measured + 8);
                float leftInset = Mathf.Max(8, NodeRadius(id) + 2);
                var bounds = new Rect(p.x - leftInset, p.y - 16, width + labelOffset + leftInset, 32);
                bool show = id == selected || !occupied.Any(rect => rect.Overlaps(bounds));
                label.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
                if (show) occupied.Add(bounds); else HiddenLabelCount++;
            }
            MarkDirtyRepaint(); Changed?.Invoke();
        }
        private Vector2 Screen(string id) => positions[id] * zoom + offset;
        private void Down(PointerDownEvent e)
        {
            if (e.button != 0 && e.button != 2) return;
            Focus(); moved = false; dragged = null;
            pressClicks = e.clickCount;
            var element = e.target as VisualElement;
            while (element != null && element != this)
            {
                if (element.name != null && element.name.StartsWith("map-node-", StringComparison.Ordinal)) { dragged = element.name.Substring(9); break; }
                element = element.parent;
            }
            if (dragged == null)
                dragged = visibleNodes.FirstOrDefault(id => Vector2.Distance(Screen(id), this.WorldToLocal(e.position)) < Mathf.Max(12, NodeRadius(id) + 3));
            pointer = e.pointerId; lastPointer = this.WorldToLocal(e.position);
            // Background and glyph drags capture immediately; label drags capture after a movement threshold.
            if (e.target == this) { this.CapturePointer(pointer); e.StopPropagation(); }
        }
        private void Move(PointerMoveEvent e)
        {
            if (pointer != e.pointerId || e.pressedButtons == 0) return;
            var p = this.WorldToLocal(e.position); var delta = p - lastPointer;
            if (!moved && delta.sqrMagnitude < 16) return;
            moved = true; this.CapturePointer(pointer);
            if (dragged == null) offset += delta; else positions[dragged] += delta / zoom;
            lastPointer = p; UpdatePositions(); e.StopPropagation();
        }
        private void Up(PointerUpEvent e)
        {
            if (pointer != e.pointerId) return;
            if (!moved && dragged != null && this.HasPointerCapture(pointer))
            {
                select(dragged);
                if (e.button == 0 && pressClicks == 2) Activated?.Invoke(dragged);
            }
            if (this.HasPointerCapture(pointer)) this.ReleasePointer(pointer);
            pointer = -1; dragged = null;
        }
        private void ComputeLayout()
        {
            var ids = graph.Nodes.Keys.OrderBy(id => id, StringComparer.Ordinal).ToArray();
            for (int i = 0; i < ids.Length; i++)
            {
                float angle = i * 2.399963f;
                positions[ids[i]] = new Vector2(Mathf.Cos(angle) * 1.7f, Mathf.Sin(angle)) * (110 + Mathf.Sqrt(i) * 72);
            }
            // Deterministic relaxation runs once, not every frame. Cycles and disconnected components are valid.
            for (int iteration = 0; iteration < 220; iteration++)
            {
                var force = ids.ToDictionary(id => id, id => -positions[id] * .007f);
                for (int i = 0; i < ids.Length; i++) for (int j = i + 1; j < ids.Length; j++)
                {
                    var d = positions[ids[i]] - positions[ids[j]];
                    var scaled = new Vector2(d.x * .52f, d.y);
                    float distance = Mathf.Max(1, scaled.magnitude);
                    var f = scaled.normalized * Mathf.Min(38, 21000 / (distance * distance));
                    f.x *= 1.8f; force[ids[i]] += f; force[ids[j]] -= f;
                }
                foreach (var r in graph.Relations)
                {
                    var d = positions[r.to] - positions[r.from];
                    var f = d.normalized * (d.magnitude - 205) * .035f;
                    force[r.from] += f; force[r.to] -= f;
                }
                foreach (var id in ids) positions[id] += Vector2.ClampMagnitude(force[id], 24) * (1 - iteration / 280f);
            }
        }
        private float NodeRadius(string id) => graph.Get(id).type == "Project" ? 11 : graph.Get(id).type == "Domain" ? 9 : 5;
        private float SelectionRadius(string id) => Mathf.Max(16, NodeRadius(id) + 8);
        private float EdgeInset(string id) => id == selected ? SelectionRadius(id) + 3 : Mathf.Max(11, NodeRadius(id) + 3);

        private void Draw(MeshGenerationContext context)
        {
            var p = context.painter2D;
            p.lineCap = LineCap.Round;
            p.lineJoin = LineJoin.Round;
            // Draw context first so focused connections stay continuous at crossings.
            for (int layer = 0; layer < 2; layer++) foreach (var r in graph.Relations)
            {
                if (!visibleNodes.Contains(r.from) || !visibleNodes.Contains(r.to)) continue;
                bool active = r.from == selected || r.to == selected;
                if (active != (layer == 1)) continue;
                var a = Screen(r.from); var b = Screen(r.to); var delta = b - a;
                float length = delta.magnitude;
                if (length < 1) continue;
                var direction = delta / length;
                // Straight by default; reciprocal connections get opposite shallow lanes.
                bool curved = curvedRelations.Contains(r.id);
                var bend = curved ? new Vector2(-direction.y, direction.x) * Mathf.Min(18, length * .055f) : Vector2.zero;
                var c1 = a + delta * .33f + bend;
                var c2 = a + delta * .67f + bend;
                var startDirection = (c1 - a).normalized;
                var endDirection = (b - c2).normalized;
                var start = a + startDirection * Mathf.Min(EdgeInset(r.from), length * .2f);
                var end = b - endDirection * Mathf.Min(EdgeInset(r.to), length * .2f);
                p.strokeColor = active ? BrainTheme.Accent : new Color32(143, 150, 163, (byte)(selected == null ? 115 : 80));
                p.lineWidth = active ? 1.65f : 1f;
                p.BeginPath(); p.MoveTo(start);
                if (curved) p.BezierCurveTo(c1, c2, end); else p.LineTo(end);
                p.Stroke();
                if (active)
                {
                    // A compact arrow follows the curve tangent and clears the node/selection ring.
                    var tangent = (end - c2).normalized;
                    var side = new Vector2(-tangent.y, tangent.x) * 2.6f;
                    p.lineWidth = 1.35f;
                    p.BeginPath(); p.MoveTo(end - tangent * 5.5f + side); p.LineTo(end); p.LineTo(end - tangent * 5.5f - side); p.Stroke();
                }
            }
            foreach (var id in visibleNodes)
            {
                var center = Screen(id); var type = graph.Get(id).type;
                p.strokeColor = p.fillColor = id == selected ? BrainTheme.Accent : new Color32(190, 194, 201, 255); p.lineWidth = 1.3f;
                if (id == selected) { p.BeginPath(); p.Arc(center, SelectionRadius(id), 0, 360); p.Stroke(); }
                p.BeginPath();
                if (type == "Evidence" || type == "Activity")
                { p.MoveTo(center + new Vector2(0,-7)); p.LineTo(center + new Vector2(7,0)); p.LineTo(center + new Vector2(0,7)); p.LineTo(center + new Vector2(-7,0)); p.ClosePath(); p.Stroke(); }
                else if (type == "Code" || type == "Document" || type == "Image" || type == "Reference")
                {
                    p.MoveTo(center + new Vector2(-6,-7)); p.LineTo(center + new Vector2(6,-7)); p.LineTo(center + new Vector2(6,7)); p.LineTo(center + new Vector2(-6,7)); p.ClosePath(); p.Stroke();
                    if (type == "Document") { p.BeginPath(); p.MoveTo(center + new Vector2(-3,-2)); p.LineTo(center + new Vector2(3,-2)); p.MoveTo(center + new Vector2(-3,2)); p.LineTo(center + new Vector2(3,2)); p.Stroke(); }
                }
                else { p.Arc(center, NodeRadius(id), 0, 360); p.Fill(); }
            }
        }
    }
}
