using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectBrain
{
    // One-hop, explicitly authored relationships. No inferred code dependencies.
    public sealed class DocumentGraphView : VisualElement
    {
        private readonly List<Vector2> endpoints = new List<Vector2>();
        private readonly List<Button> nodes = new List<Button>();
        private Vector2 center;

        public DocumentGraphView(ScriptDocument document, Action<MonoScript> select)
        {
            style.minHeight = 320;
            style.flexGrow = 1;
            style.backgroundColor = new Color(0.075f, 0.085f, 0.11f);
            style.overflow = Overflow.Hidden;
            var guids = new[] { document.scriptGuid }.Concat(document.relatedScriptGuids ?? Array.Empty<string>()).Distinct().ToArray();
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                var button = new Button(() => { if (script != null) select(script); })
                {
                    text = script == null ? "누락된 코드" : Path.GetFileNameWithoutExtension(path),
                    tooltip = script == null ? guid : path,
                    name = "node-" + guid
                };
                button.SetEnabled(script != null);
                button.style.position = Position.Absolute;
                button.style.width = 140;
                button.style.height = 42;
                button.style.marginLeft = button.style.marginTop = 0;
                button.style.backgroundColor = guid == document.scriptGuid ? new Color(0.36f, 0.24f, 0.65f) : new Color(0.15f, 0.22f, 0.3f);
                button.style.color = Color.white;
                nodes.Add(button);
                Add(button);
            }
            RegisterCallback<GeometryChangedEvent>(_ => LayoutNodes());
            generateVisualContent += context =>
            {
                var painter = context.painter2D;
                painter.strokeColor = new Color(0.4f, 0.5f, 0.7f);
                painter.lineWidth = 2;
                foreach (var end in endpoints)
                {
                    painter.BeginPath();
                    painter.MoveTo(center);
                    painter.LineTo(end);
                    painter.Stroke();
                }
            };
        }

        private void LayoutNodes()
        {
            center = new Vector2(contentRect.width / 2, contentRect.height / 2);
            endpoints.Clear();
            for (int i = 0; i < nodes.Count; i++)
            {
                var point = center;
                if (i > 0)
                {
                    float angle = (i - 1) * Mathf.PI * 2 / (nodes.Count - 1) - Mathf.PI / 2;
                    point += new Vector2(Mathf.Cos(angle) * Mathf.Max(0, center.x - 85), Mathf.Sin(angle) * Mathf.Max(0, center.y - 45));
                    endpoints.Add(point);
                }
                nodes[i].style.left = point.x - 70;
                nodes[i].style.top = point.y - 21;
            }
            MarkDirtyRepaint();
        }
    }
}
