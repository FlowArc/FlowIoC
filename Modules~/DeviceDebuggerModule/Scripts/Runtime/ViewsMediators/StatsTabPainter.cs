using System;
using System.Globalization;
using Modules.DeviceDebuggerModule.Data.ValueObjects;
using UnityEngine;
using UnityEngine.UIElements;

namespace Modules.DeviceDebuggerModule.ViewsMediators
{
    /// <summary>
    /// The Stats page: a strip of frame-time bars drawn with Painter2D, a guide at the game's own
    /// frame target, and the numbers under it. Repainted with whatever sample it is handed; the
    /// graph's ceiling comes from the config.
    /// </summary>
    public class StatsTabPainter
    {
        private readonly VisualElement _graph;
        private readonly Label _rows;
        private float[] _history = Array.Empty<float>();
        private float _guideMs;
        private float _ceilingMs;

        public StatsTabPainter(VisualElement page)
        {
            _graph = page.Q<VisualElement>("stats-graph");
            _rows = page.Q<Label>("stats-rows");
            _graph.generateVisualContent += DrawGraph;
        }

        /// <summary>The frame time drawn at the top of the graph; a longer frame is drawn at full height.</summary>
        public void SetGraphCeiling(float ceilingMs)
        {
            _ceilingMs = ceilingMs;
            _graph.MarkDirtyRepaint();
        }

        public void Paint(StatsSampleVO sample)
        {
            if (sample == null) return;

            _history = sample.FrameHistory ?? Array.Empty<float>();
            _guideMs = sample.TargetFrameMs;
            _graph.MarkDirtyRepaint();

            _rows.text =
                "FPS  " + sample.Fps.ToString("0", CultureInfo.InvariantCulture) + "   frame " +
                sample.FrameMs.ToString("0.0", CultureInfo.InvariantCulture) + " ms   worst " +
                sample.WorstFrameMs.ToString("0.0", CultureInfo.InvariantCulture) + " ms\n"
                + "Allocated  " + Megabytes(sample.AllocatedBytes) + "   reserved " + Megabytes(sample.ReservedBytes) + "\n"
                + "Mono heap  " + Megabytes(sample.MonoHeapBytes) + "   GC runs " + sample.GcCount + "\n"
                + "Uptime  " + TimeSpan.FromSeconds(sample.Uptime).ToString(@"hh\:mm\:ss") + "   time scale " +
                sample.TimeScale.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private void DrawGraph(MeshGenerationContext context)
        {
            Painter2D painter = context.painter2D;
            Rect area = _graph.contentRect;

            if (area.width <= 0f || area.height <= 0f || _ceilingMs <= 0f || _guideMs <= 0f) return;

            // The guide at the game's own frame target.
            float guideY = area.yMax - Mathf.Clamp01(_guideMs / _ceilingMs) * area.height;
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
                float height = Mathf.Clamp01(ms / _ceilingMs) * area.height;
                float x = area.xMin + i * barWidth + barWidth * 0.5f;

                painter.strokeColor = ms > _guideMs * 2f ? new Color(0.88f, 0.32f, 0.31f)
                    : ms > _guideMs ? new Color(0.90f, 0.71f, 0.33f)
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