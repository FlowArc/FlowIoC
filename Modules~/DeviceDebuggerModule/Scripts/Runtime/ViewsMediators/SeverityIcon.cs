using UnityEngine;
using UnityEngine.UIElements;

namespace Modules.DeviceDebuggerModule.ViewsMediators
{
    /// <summary>
    /// The kind of a row at a glance, the way Unity's console leads a row with an icon: a red
    /// disc with a bar for an error, an amber triangle for a warning, a grey-blue disc with an i
    /// for a plain line. Drawn with Painter2D, because a player has none of the Editor's icons
    /// and a texture would be one more asset to ship.
    /// </summary>
    public class SeverityIcon : VisualElement
    {
        private static readonly Color ErrorColor = new(0.88f, 0.32f, 0.31f);
        private static readonly Color WarningColor = new(0.90f, 0.71f, 0.33f);
        private static readonly Color LogColor = new(0.45f, 0.55f, 0.68f);
        private static readonly Color Ink = new(0.08f, 0.09f, 0.11f);

        private LogType _kind = LogType.Log;

        public SeverityIcon()
        {
            generateVisualContent += Draw;
            pickingMode = PickingMode.Ignore;
        }

        public LogType Kind
        {
            get => _kind;
            set
            {
                if (_kind == value) return;

                _kind = value;
                MarkDirtyRepaint();
            }
        }

        private void Draw(MeshGenerationContext context)
        {
            Painter2D painter = context.painter2D;
            Rect area = contentRect;

            if (area.width <= 0f || area.height <= 0f) return;

            float size = Mathf.Min(area.width, area.height);
            var center = new Vector2(area.xMin + area.width * 0.5f, area.yMin + area.height * 0.5f);
            float radius = size * 0.5f;

            switch (_kind)
            {
                case LogType.Warning:
                    Triangle(painter, center, radius, WarningColor);
                    Mark(painter, center + new Vector2(0f, radius * 0.12f), radius * 0.62f, Ink);
                    break;
                case LogType.Log:
                    Disc(painter, center, radius, LogColor);
                    Dot(painter, center - new Vector2(0f, radius * 0.42f), radius * 0.13f, Color.white);
                    Bar(painter, center + new Vector2(0f, radius * 0.16f), radius * 0.5f, radius * 0.22f, Color.white);
                    break;
                default:
                    Disc(painter, center, radius, ErrorColor);
                    Mark(painter, center, radius * 0.6f, Color.white);
                    break;
            }
        }

        private static void Disc(Painter2D painter, Vector2 center, float radius, Color color)
        {
            painter.fillColor = color;
            painter.BeginPath();
            painter.Arc(center, radius, 0f, 360f);
            painter.ClosePath();
            painter.Fill();
        }

        private static void Triangle(Painter2D painter, Vector2 center, float radius, Color color)
        {
            painter.fillColor = color;
            painter.BeginPath();
            painter.MoveTo(center + new Vector2(0f, -radius));
            painter.LineTo(center + new Vector2(radius, radius * 0.8f));
            painter.LineTo(center + new Vector2(-radius, radius * 0.8f));
            painter.ClosePath();
            painter.Fill();
        }

        /// <summary>An exclamation mark: a bar and a dot under it, centred on the point.</summary>
        private static void Mark(Painter2D painter, Vector2 center, float height, Color color)
        {
            float width = height * 0.28f;
            Bar(painter, center - new Vector2(0f, height * 0.18f), height * 0.6f, width, color);
            Dot(painter, center + new Vector2(0f, height * 0.42f), width * 0.55f, color);
        }

        private static void Bar(Painter2D painter, Vector2 center, float height, float width, Color color)
        {
            painter.fillColor = color;
            painter.BeginPath();
            painter.MoveTo(center + new Vector2(-width * 0.5f, -height * 0.5f));
            painter.LineTo(center + new Vector2(width * 0.5f, -height * 0.5f));
            painter.LineTo(center + new Vector2(width * 0.5f, height * 0.5f));
            painter.LineTo(center + new Vector2(-width * 0.5f, height * 0.5f));
            painter.ClosePath();
            painter.Fill();
        }

        private static void Dot(Painter2D painter, Vector2 center, float radius, Color color)
        {
            painter.fillColor = color;
            painter.BeginPath();
            painter.Arc(center, radius, 0f, 360f);
            painter.ClosePath();
            painter.Fill();
        }
    }
}
