using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectBrain
{
    // Stored containment only. Connector direction is hierarchy, never an inferred call sequence.
    internal sealed class BrainArchitectureView : VisualElement
    {
        private readonly VisualElement origin;
        private readonly List<VisualElement> branches = new List<VisualElement>();
        internal BrainArchitectureView(BrainGraphService graph, string id, Action<string> open)
        {
            AddToClassList("architecture-diagram");
            var node = graph.Get(id);
            origin = new VisualElement(); origin.AddToClassList("architecture-origin"); Add(origin);
            origin.Add(BrainPresentation.Text(BrainTheme.TypeName(node.type), "reader-caption"));
            origin.Add(BrainPresentation.Text(node.title, "reader-heading"));
            var children = graph.Children(id);
            if (children.Length == 0) children = graph.Relations.Where(r => r.from == id && r.type == "implemented_by").Select(r => r.to).ToArray();
            var grid = new VisualElement(); grid.AddToClassList("architecture-grid"); Add(grid);
            foreach (var child in children)
            {
                var item = graph.Get(child); var contents = BrainPresentation.Contents(graph, child);
                var button = new Button(() => open(child)); button.AddToClassList("architecture-branch");
                button.Add(BrainPresentation.Text(BrainTheme.TypeName(item.type), "reader-caption"));
                button.Add(BrainPresentation.Text(item.title, "reader-heading"));
                button.Add(BrainPresentation.Text(BrainPresentation.Summary(item.summary, 85)));
                int codes = contents.Count(x => graph.Get(x).type == "Code");
                if (codes > 0) button.Add(BrainPresentation.Text("구현 코드 " + codes + "개  →", "architecture-count"));
                grid.Add(button); branches.Add(button);
            }
            if (children.Length == 0) grid.Add(BrainPresentation.Text("하위 구성이 아직 연결되지 않았습니다."));
            int columns = 0;
            grid.RegisterCallback<GeometryChangedEvent>(e =>
            {
                int next = e.newRect.width >= 850 ? 5 : e.newRect.width >= 630 ? 3 : e.newRect.width >= 350 ? 2 : 1;
                if (next == columns) return; columns = next;
                foreach (var branch in branches) { branch.style.flexGrow = 0; branch.style.flexBasis = new Length(100f / columns - 1.6f, LengthUnit.Percent); }
                MarkDirtyRepaint();
            });
            RegisterCallback<GeometryChangedEvent>(_ => MarkDirtyRepaint());
            foreach (var branch in branches) branch.RegisterCallback<GeometryChangedEvent>(_ => MarkDirtyRepaint());
            generateVisualContent += context =>
            {
                if (origin.layout.width <= 0) return;
                var p = context.painter2D; p.strokeColor = new Color32(130, 149, 188, 170); p.lineWidth = 1.4f;
                var from = origin.ChangeCoordinatesTo(this, new Vector2(0, origin.layout.height * .5f));
                float bus = 9;
                foreach (var branch in branches)
                {
                    var to = branch.ChangeCoordinatesTo(this, new Vector2(0, branch.layout.height * .5f));
                    p.BeginPath(); p.MoveTo(from); p.LineTo(new Vector2(bus, from.y)); p.LineTo(new Vector2(bus, to.y)); p.LineTo(to); p.Stroke();
                }
            };
        }
    }
}
