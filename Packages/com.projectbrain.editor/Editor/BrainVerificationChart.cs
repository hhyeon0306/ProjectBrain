using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectBrain
{
    internal sealed class BrainVerificationChart : VisualElement
    {
        internal BrainVerificationChart(BrainVerificationRecord run)
        {
            AddToClassList("verification-chart");
            var count = BrainPresentation.Text(run.passed + " / " + run.total, "verification-chart-count"); Add(count);
            Add(BrainPresentation.Text("실행 당시 통과", "verification-chart-caption"));
            generateVisualContent += context =>
            {
                var p = context.painter2D; var center = new Vector2(contentRect.width * .5f, 83); float radius = 63;
                p.lineWidth = 9; p.lineCap = LineCap.Butt;
                p.strokeColor = new Color32(67, 75, 89, 255); p.BeginPath(); p.Arc(center, radius, -90, 270); p.Stroke();
                if (run.total <= 0) return;
                float start = -90;
                var values = new[] { run.passed, run.failed, run.skipped };
                var colors = new[] { new Color32(142, 195, 178, 255), new Color32(221, 140, 148, 255), new Color32(215, 185, 131, 255) };
                for (int i = 0; i < values.Length; i++)
                {
                    float sweep = Mathf.Clamp(values[i] / (float)run.total * 360, 0, 360);
                    if (sweep <= 0) continue;
                    p.strokeColor = colors[i]; p.BeginPath(); p.Arc(center, radius, start, start + sweep); p.Stroke(); start += sweep;
                }
            };
        }
    }
}
