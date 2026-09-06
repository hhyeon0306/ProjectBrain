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

        public DocumentGraphView(ScriptDocument document, string[] relatedGuids, Action<MonoScript> select)
        {
            name = "document-graph";
            style.minHeight = 320;
            style.flexShrink = 0;
            style.backgroundColor = (Color)new Color32(48, 50, 52, 255);
            style.overflow = Overflow.Hidden;
            var guids = new[] { document.scriptGuid }.Concat(relatedGuids ?? Array.Empty<string>()).Distinct().ToArray();
            style.height = Mathf.Max(320, (guids.Length - 1) * 76 + 40);
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
                button.style.width = 220;
                button.style.height = 52;
                button.style.whiteSpace = WhiteSpace.Normal;
                button.style.fontSize = 13;
                button.style.paddingLeft = button.style.paddingRight = 10;
                button.style.marginLeft = button.style.marginTop = 0;
                button.style.backgroundColor = guid == document.scriptGuid ? BrainTheme.Accent : (Color)new Color32(59, 61, 64, 255);
                button.style.color = guid == document.scriptGuid ? (Color)new Color32(37, 40, 51, 255) : Color.white;
                nodes.Add(button);
                Add(button);
            }
            RegisterCallback<GeometryChangedEvent>(_ => LayoutNodes());
            generateVisualContent += context =>
            {
                var painter = context.painter2D;
                painter.strokeColor = new Color(0.4f, 0.5f, 0.7f);
                painter.lineWidth = 1.2f;
                foreach (var end in endpoints)
                {
                    painter.BeginPath();
                    painter.MoveTo(center);
                    painter.BezierCurveTo(center + new Vector2(70,0), end - new Vector2(70,0), end);
                    painter.Stroke();
                }
            };
        }

        private void LayoutNodes()
        {
            if (float.IsNaN(contentRect.width) || contentRect.width < 1) return;
            float width = Mathf.Clamp(contentRect.width * .36f, 150, 240);
            center = new Vector2(24 + width, contentRect.height / 2);
            endpoints.Clear();
            for (int i = 0; i < nodes.Count; i++)
            {
                float left = i == 0 ? 24 : Mathf.Max(24 + width + 60, contentRect.width - width - 24);
                float top = i == 0 ? center.y - 26 : 20 + (i - 1) * 76;
                nodes[i].style.width = width;
                nodes[i].style.left = left;
                nodes[i].style.top = top;
                if (i > 0) endpoints.Add(new Vector2(left, top + 26));
            }
            MarkDirtyRepaint();
        }
    }
}
