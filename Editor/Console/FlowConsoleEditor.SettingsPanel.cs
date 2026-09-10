#if UNITY_EDITOR
using System;
using FlowIoC.ConsoleModule;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.Console
{
    /// <summary>
    /// How a row is drawn, and how much a log is asked to remember about where it came from. These
    /// are settings a reader changes while following one flow and then puts back, which is why they
    /// are here rather than three windows away in the asset - but they are not read every minute,
    /// so they are folded behind the gear until it is pressed.
    /// </summary>
    internal partial class FlowConsoleEditor
    {
        /// <summary>The toolbar's height, so the strip reads as a piece of it.</summary>
        private const float FloatingStripHeight = 20f;

        private const float SettingsButtonWidth = 30f;
        private const float RowLinesWidth = 60f;
        private const float SourceCaptureWidth = 150f;
        private const float ExportWidth = 60f;

        /// <summary>
        /// The grey the filters panel's bar comes out as, but opaque: the bar is a shade over the
        /// window, and a shade over the list would take the colour of whichever row - banded,
        /// selected - happened to lie under it.
        /// </summary>
        private static readonly Color FloatingStripColor = new Color(0.13f, 0.13f, 0.13f, 1f);

        /// <summary>
        /// The gear, taken from the editor's own icons so it matches every other settings button in
        /// Unity. Which name carries it has moved between versions, so the first one that answers
        /// wins and the word is what is left if none of them do.
        ///
        /// Built on every draw rather than kept. The texture behind an editor icon belongs to the
        /// editor and is freed on a domain reload, so a GUIContent holding one drew a button with
        /// no icon and no word in it - the shape of a button and nothing inside.
        /// </summary>
        private GUIContent SettingsButtonContent()
        {
            const string tooltip = "Show how a row is drawn and how much a log remembers about where it came from.";

            foreach (string iconName in SettingsIconNames)
            {
                Texture icon = EditorGUIUtility.IconContent(iconName)?.image;
                if (icon == null) continue;

                return new GUIContent(icon, tooltip);
            }

            return new GUIContent("Settings", tooltip);
        }

        private static readonly string[] SettingsIconNames =
        {
            "d__Popup", "_Popup", "SettingsIcon", "d_SettingsIcon", "EditorSettings Icon"
        };

        /// <summary>
        /// The strip that lies over the top-right corner of the list: the gear, the three controls
        /// it opens, and the filters switch while its panel is closed. They used to have a bar of
        /// their own under the toolbar, empty from its left edge to the gear, and the bar cost the
        /// list a row. Now the list runs up to the toolbar and the strip floats on it. Pressing the
        /// gear grows the strip leftwards over the rows rather than bringing the bar back, so the
        /// list never moves under the reader.
        ///
        /// Drawn after the list so that it paints over the rows, which is also why a row lying under
        /// it has to be told to let a press go by - see <see cref="_pointerOverFloatingStrip"/>.
        /// </summary>
        private void FloatingStripGUI(Rect viewport)
        {
            // While the list overflows the scrollbar owns the right edge, and the strip steps to
            // its left rather than covering the top of the thumb.
            float right = viewport.xMax;
            if (ContentHeight() > viewport.height) right -= GUI.skin.verticalScrollbar.fixedWidth;

            float width = SettingsButtonWidth;
            if (_showSettings) width += RowLinesWidth + SourceCaptureWidth + ExportWidth;
            if (!_showFilters) width += FiltersPanelWidth;

            var strip = new Rect(right - width, viewport.y, width, FloatingStripHeight);
            _floatingStripRect = strip;

            // The toolbar's grey is four levels off the rows', and a strip painted in it was lost
            // on them. It is painted in the filters panel's bar instead, so it reads as the same
            // chrome floating on the list; the toolbar buttons on it paint no ground of their own,
            // only their edges and their lit states. Darker rather than lighter, because lit is
            // what a toolbar button turns when it is on, and a brighter strip would read as five
            // pressed buttons with the one that is actually pressed lost among them.
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(strip, FloatingStripColor);

            float x = strip.x;

            if (_showSettings)
            {
                RowLinesSettingGUI(new Rect(x, strip.y, RowLinesWidth, strip.height));
                x += RowLinesWidth;

                SourceCaptureSettingGUI(new Rect(x, strip.y, SourceCaptureWidth, strip.height));
                x += SourceCaptureWidth;

                ExportMenuGUI(new Rect(x, strip.y, ExportWidth, strip.height));
                x += ExportWidth;
            }

            SettingsButtonGUI(new Rect(x, strip.y, SettingsButtonWidth, strip.height));
            x += SettingsButtonWidth;

            if (!_showFilters)
                FiltersToggleGUI(new Rect(x, strip.y, FiltersPanelWidth, strip.height));

            // The edge the filters panel draws down its side, along the strip's left and bottom,
            // so it ends the way the panel does rather than fading into the row behind it.
            if (Event.current.type != EventType.Repaint) return;

            EditorGUI.DrawRect(new Rect(strip.x, strip.y, 1f, strip.height), FiltersPanelEdgeColor);
            EditorGUI.DrawRect(new Rect(strip.x, strip.yMax - 1f, strip.width, 1f), FiltersPanelEdgeColor);
        }

        private void SettingsButtonGUI(Rect rect)
        {
            bool showSettings = GUI.Toggle(rect, _showSettings, SettingsButtonContent(), EditorStyles.toolbarButton);

            if (showSettings == _showSettings) return;

            _showSettings = showSettings;
            _state.ShowSettings = showSettings;
            _needsRepaint = true;
        }

        private void RowLinesSettingGUI(Rect rect)
        {
            var content = new GUIContent(_rowLineCount + " line" + (_rowLineCount == 1 ? "" : "s"),
                "How many lines a row shows. Two is Unity's shape: the message, and underneath it "
                + "where the message came from.");

            if (!GUI.Button(rect, content, EditorStyles.toolbarDropDown)) return;

            var menu = new GenericMenu();

            for (int lines = 1; lines <= 3; lines++)
            {
                int chosen = lines;
                menu.AddItem(new GUIContent(lines + " line" + (lines == 1 ? "" : "s")),
                    _rowLineCount == lines,
                    () =>
                    {
                        _rowLineCount = chosen;
                        _state.RowLineCount = chosen;
                        _logsDirty = true;
                        _needsRepaint = true;
                    });
            }

            menu.DropDown(new Rect(rect.x, rect.yMax, 0f, 0f));
        }

        /// <summary>
        /// The setting that decides whether double-clicking a row can go anywhere. Raised while
        /// following one flow and lowered afterwards, because working out a source builds the whole
        /// managed stack as a string and the framework logs every signal, injection and command.
        /// </summary>
        private void SourceCaptureSettingGUI(Rect rect)
        {
            FlowStackTraceCapture capture = _settings.StackTraceCapture;

            var content = new GUIContent("Source: " + capture,
                "Which logs work out where they came from.");

            if (!GUI.Button(rect, content, EditorStyles.toolbarDropDown)) return;

            var menu = new GenericMenu();

            foreach (FlowStackTraceCapture value in Enum.GetValues(typeof(FlowStackTraceCapture)))
            {
                FlowStackTraceCapture chosen = value;
                menu.AddItem(new GUIContent(CaptureLabel(value)), capture == value, () =>
                {
                    _settings.StackTraceCapture = chosen;
                    EditorUtility.SetDirty(_settings);
                    _needsRepaint = true;
                });
            }

            menu.DropDown(new Rect(rect.x, rect.yMax, 0f, 0f));
        }
    }
}
#endif