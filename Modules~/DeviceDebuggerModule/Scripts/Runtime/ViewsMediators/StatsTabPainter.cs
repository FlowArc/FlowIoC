using System;
using System.Globalization;
using Modules.DeviceDebuggerModule.Data.ValueObjects;
using UnityEngine;
using UnityEngine.UIElements;

namespace Modules.DeviceDebuggerModule.ViewsMediators
{
    /// <summary>
    /// The Stats page: a strip of frame-time bars drawn with Painter2D, a guide at 16.7 ms, and
    /// the numbers under it. Repainted with whatever sample it is handed.
    /// </summary>
    public class StatsTabPainter
    {
        private const float GUIDE_MS = 1000f / 60f;
        private const float GRAPH_CEILING_MS = 50f;

        private readonly VisualElement _graph;
        private readonly Label _rows;
        private float[] _history = Array.Empty<float>();

        public StatsTabPainter(VisualElement page)
        {
            _graph = page.Q<VisualElement>("stats-graph");
            _rows = page.Q<Label>("stats-rows");
            _graph.generateVisualContent += DrawGraph;
        }

        public void Paint(StatsSampleVO sample)
        {
            if (sample == null) return;

            _history = sample.FrameHistory ?? Array.Empty<float>();
            _graph.MarkDirtyRepaint();

            _rows.text =
                "FPS  " + sample.Fps.ToString("0", CultureInfo.InvariantCulture) + "   frame " + sample.FrameMs.ToString("0.0", CultureInfo.InvariantCulture) + " ms   worst " + sample.WorstFrameMs.ToString("0.0", CultureInfo.InvariantCulture) + " ms\n"
                + "Allocated  " + Megabytes(sample.AllocatedBytes) + "   reserved " + Megabytes(sample.ReservedBytes) + "\n"
                + "Mono heap  " + Megabytes(sample.MonoHeapBytes) + "   GC runs " + sample.GcCount + "\n"
                + "Uptime  " + TimeSpan.FromSeconds(sample.Uptime).ToString(@"hh\:mm\:ss") + "   time scale " + sample.TimeScale.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private void DrawGraph(MeshGenerationContext context)
        {
            Painter2D painter = context.painter2D;
            Rect area = _graph.contentRect;

            if (area.width <= 0f || area.height <= 0f) return;

            // The 60 fps guide.
            float guideY = area.yMax - Mathf.Clamp01(GUIDE_MS / GRAPH_CEILING_MS) * area.height;
            painter.strokeColor = new Color(1f, 1f, 1f, 0.25f);
            painter.lineWidth = 1f;
            painter.BeginPath();
            painter.MoveTo(new Vector2(area.xMin, guideY));
            painter.LineTo(new Vector2(area.xMax, guideY));
            painter.Stroke();

            if (_history.Length == 0) return;

            float barWidth = area.width / _history.Length;
            painter.lineWidth = Mathf.Max(1f, barWidth - 1f);

            for (int i = 0; i < _history.Length; i++)
            {
                float ms = _history[i];
                float height = Mathf.Clamp01(ms / GRAPH_CEILING_MS) * area.height;
                float x = area.xMin + i * barWidth + barWidth * 0.5f;

                painter.strokeColor = ms > GUIDE_MS * 2f ? new Color(0.88f, 0.32f, 0.31f)
                    : ms > GUIDE_MS ? new Color(0.90f, 0.71f, 0.33f)
                    : new Color(0.42f, 0.78f, 0.55f);
                painter.BeginPath();
                painter.MoveTo(new Vector2(x, area.yMax));
                painter.LineTo(new Vector2(x, area.yMax - height));
                painter.Stroke();
            }
        }

        private static string Megabytes(long bytes) => (bytes / (1024f * 1024f)).ToString("0.0", CultureInfo.InvariantCulture) + " MB";
    }
}
