#if UNITY_EDITOR
using System;
using FlowIoC.ConsoleModule;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.Console
{
    /// <summary>
    /// How the list is viewed: which rows it shows and how they are grouped and led, how a row is
    /// drawn, and how much a log is asked to remember about where it came from. These are switches
    /// a reader throws while following one flow and puts back afterwards, which is why they are
    /// here rather than three windows away in the asset - but they are not read every minute, so
    /// they are folded behind the gear until it is pressed, and the toolbar keeps the actions.
    /// </summary>
    internal partial class FlowConsoleEditor
    {
        /// <summary>The toolbar's height, so the strip reads as a piece of it.</summary>
        private const float FloatingStripHeight = 20f;

        private const float SettingsButtonWidth = 30f;
        private const float RowLinesWidth = 60f;
        private const float SourceCaptureWidth = 150f;
        private const float ExportWidth = 60f;

        private const float FlowToggleWidth = 50f;
        private const float PinnedToggleWidth = 76f;
        private const float TimeFormatWidth = 58f;

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
        /// The strip that lies over the top-right corner of the list while the gear is closed: the
        /// gear itself, and the filters switch while its panel is closed. Nothing else needs a row
        /// while the gear is closed, so the list runs up to the toolbar and the strip floats on it.
        /// Pressing the gear opens <see cref="SettingsBarGUI"/> in its place.
        ///
        /// Drawn after the list so that it paints over the rows, which is also why a row lying under
        /// it has to be told to let a press go by - see <see cref="_pointerOverStrip"/>.
        /// </summary>
        private void FloatingStripGUI(Rect viewport)
        {
            // While the list overflows the scrollbar owns the right edge, and the strip steps to
            // its left rather than covering the top of the thumb.
            float right = viewport.xMax;
            if (ContentHeight() > viewport.height) right -= GUI.skin.verticalScrollbar.fixedWidth;

            float width = RightControlsWidth();

            var strip = new Rect(right - width, viewport.y, width, FloatingStripHeight);
            _stripRect = strip;

            // The toolbar's grey is four levels off the rows', and a strip painted in it was lost
            // on them. It is painted in the filters panel's bar instead, so it reads as the same
            // chrome floating on the list; the toolbar buttons on it paint no ground of their own,
            // only their edges and their lit states. Darker rather than lighter, because lit is
            // what a toolbar button turns when it is on, and a brighter strip would read as five
            // pressed buttons with the one that is actually pressed lost among them.
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(strip, FloatingStripColor);

            RightControlsGUI(strip);

            // The edge the filters panel draws down its side, along the strip's left and bottom,
            // so it ends the way the panel does rather than fading into the row behind it.
            if (Event.current.type != EventType.Repaint) return;

            EditorGUI.DrawRect(new Rect(strip.x, strip.y, 1f, strip.height), FiltersPanelEdgeColor);
            EditorGUI.DrawRect(new Rect(strip.x, strip.yMax - 1f, strip.width, 1f), FiltersPanelEdgeColor);
        }

        /// <summary>
        /// The bar the gear opens: a row of its own under the toolbar, as wide as the list, so the
        /// rows start under it rather than lying beneath it. What shapes the rows sits at its left
        /// end, in the order the owner set - Time, the line count, Flow, Pinned - where the eye
        /// starts a bar; Source and Export sit at the right with the gear that closes it, so the
        /// gear is in the same place open and closed. The filters switch heads its own panel, or
        /// sits beside the gear while the panel is closed, the way it does on the strip.
        ///
        /// A bar too narrow for all of it drops controls whole rather than drawing them half, the
        /// way the toolbar does: the right group first - Source, then Export - and then the left
        /// group from its right end - Pinned, Flow, the line count, Time. The gear and the filters
        /// switch are never dropped; they are how the bar is closed and the panel opened.
        ///
        /// Painted as a toolbar, in the toolbar's own grey: it is a second row of the toolbar, not
        /// a thing lying on the rows, so it needs none of the darkness the floating strip needs to
        /// be picked out of them.
        /// </summary>
        private void SettingsBarGUI()
        {
            Rect bar = GUILayoutUtility.GetRect(0f, FloatingStripHeight,
                GUILayout.ExpandWidth(true), GUILayout.Height(FloatingStripHeight));

            // Kept only from a repaint: a layout pass answers with a placeholder rect.
            if (Event.current.type == EventType.Repaint)
            {
                _stripRect = bar;
                EditorStyles.toolbar.Draw(bar, false, false, false, false);
            }

            // What always stays, then what fits after it, taken in the order it is kept - the
            // reverse of the order it is dropped. Once one does not fit, none after it is drawn
            // either, so the bar never shows a less important control in place of a more
            // important one that happened to be wider.
            float room = bar.width - SettingsButtonWidth - (_showFilters ? 0f : _filtersPanelWidth);
            float used = 0f;

            bool showTime = Fits(TimeFormatWidth, ref used, room);
            bool showLines = showTime && Fits(RowLinesWidth, ref used, room);
            bool showFlow = showLines && Fits(FlowToggleWidth, ref used, room);
            bool showPinned = showFlow && Fits(PinnedToggleWidth, ref used, room);
            bool showExport = showPinned && Fits(ExportWidth, ref used, room);
            bool showSource = showExport && Fits(SourceCaptureWidth, ref used, room);

            float x = bar.x;

            if (showTime)
            {
                TimeFormatGUI(new Rect(x, bar.y, TimeFormatWidth, bar.height));
                x += TimeFormatWidth;
            }

            if (showLines)
            {
                RowLinesSettingGUI(new Rect(x, bar.y, RowLinesWidth, bar.height));
                x += RowLinesWidth;
            }

            if (showFlow)
            {
                FlowToggleGUI(new Rect(x, bar.y, FlowToggleWidth, bar.height));
                x += FlowToggleWidth;
            }

            if (showPinned)
                PinnedToggleGUI(new Rect(x, bar.y, PinnedToggleWidth, bar.height));

            RightControlsGUI(bar, showSource, showExport);
        }

        /// <summary>Whether a control of this width still fits, and the room it takes if it does.</summary>
        private static bool Fits(float width, ref float used, float room)
        {
            if (used + width > room) return false;

            used += width;
            return true;
        }

        /// <summary>
        /// How wide the floating strip is: the gear, and the filters switch while its panel is
        /// closed. The bar is as wide as the list and lays its own controls out.
        /// </summary>
        private float RightControlsWidth()
        {
            float width = SettingsButtonWidth;
            if (!_showFilters) width += _filtersPanelWidth;
            return width;
        }

        /// <summary>
        /// The controls at the right end of the strip and the bar alike, laid from the right edge
        /// leftwards so they end where the list does: the filters switch, the gear, and on the bar
        /// Export and Source to the gear's left, each when the bar has room for it.
        /// </summary>
        private void RightControlsGUI(Rect strip, bool showSource = false, bool showExport = false)
        {
            float x = strip.xMax;

            if (!_showFilters)
            {
                x -= _filtersPanelWidth;
                FiltersToggleGUI(new Rect(x, strip.y, _filtersPanelWidth, strip.height));
            }

            x -= SettingsButtonWidth;
            SettingsButtonGUI(new Rect(x, strip.y, SettingsButtonWidth, strip.height));

            if (showExport)
            {
                x -= ExportWidth;
                ExportMenuGUI(new Rect(x, strip.y, ExportWidth, strip.height));
            }

            if (showSource)
            {
                x -= SourceCaptureWidth;
                SourceCaptureSettingGUI(new Rect(x, strip.y, SourceCaptureWidth, strip.height));
            }
        }

        private void SettingsButtonGUI(Rect rect)
        {
            bool showSettings = GUI.Toggle(rect, _showSettings, SettingsButtonContent(), EditorStyles.toolbarButton);

            if (showSettings == _showSettings) return;

            _showSettings = showSettings;
            _state.ShowSettings = showSettings;
            _needsRepaint = true;
        }

        private void FlowToggleGUI(Rect rect)
        {
            var flowLabel = new GUIContent("Flow",
                "Group the rows into the flows they belong to. A flow started from inside another sits under it.");

            bool flowMode = GUI.Toggle(rect, _flowMode, flowLabel, EditorStyles.toolbarButton);
            if (flowMode == _flowMode) return;

            _flowMode = flowMode;
            _state.FlowMode = flowMode;
            _logsDirty = true;
            _needsRepaint = true;
        }

        private void PinnedToggleGUI(Rect rect)
        {
            // Fetched every draw, never kept: the texture behind an editor icon is freed on a
            // domain reload and a GUIContent holding one draws nothing.
            var pinnedLabel = new GUIContent(" Pinned", EditorGUIUtility.IconContent("pin")?.image,
                "Show only the rows you pinned. Pin one with the row's right-click menu, or with P.");

            // Tinted rather than drawn behind: a toolbar button paints its own pressed background
            // and covered anything under it. GUI.backgroundColor multiplies that background, so
            // the switch takes the same colour the pinned rows carry.
            Color backgroundWas = GUI.backgroundColor;
            if (_pinnedOnly) GUI.backgroundColor = PinBadgeTintColor;

            bool pinnedOnly = GUI.Toggle(rect, _pinnedOnly, pinnedLabel, EditorStyles.toolbarButton);

            GUI.backgroundColor = backgroundWas;
            if (pinnedOnly == _pinnedOnly) return;

            _pinnedOnly = pinnedOnly;
            _logsDirty = true;
            _needsRepaint = true;
        }

        /// <summary>
        /// What leads a row. A menu rather than a switch because there are three answers: the
        /// clock to the second the way Unity's console leads a row, the clock with its
        /// milliseconds, or the frame and the gap since the row above.
        /// </summary>
        private void TimeFormatGUI(Rect rect)
        {
            // The word alone, not the format: the rows beside it already show which one is on,
            // and the menu ticks it. Keeps the button as narrow as the line count beside it.
            var content = new GUIContent("Time",
                "What leads a row: the clock to the second, as Unity's console shows it; the clock "
                + "with its milliseconds; or the frame the row was written in and the gap since the "
                + "row above.");

            if (!GUI.Button(rect, content, EditorStyles.toolbarDropDown)) return;

            var menu = new GenericMenu();

            foreach (FlowConsoleTimeFormat value in Enum.GetValues(typeof(FlowConsoleTimeFormat)))
            {
                FlowConsoleTimeFormat chosen = value;
                menu.AddItem(new GUIContent(TimeFormatLabel(value)), _timeFormat == value, () =>
                {
                    _timeFormat = chosen;
                    _state.TimeFormat = chosen;
                    _needsRepaint = true;
                });
            }

            menu.DropDown(new Rect(rect.x, rect.yMax, 0f, 0f));
        }

        /// <summary>The menu shows what each format looks like, so nobody has to try all three.</summary>
        private static string TimeFormatLabel(FlowConsoleTimeFormat format)
        {
            switch (format)
            {
                case FlowConsoleTimeFormat.Classic: return "Classic     13:05:23";
                case FlowConsoleTimeFormat.Extended: return "Extended   13:05:23:088";
                case FlowConsoleTimeFormat.Frame: return "Frame       f120  +12ms";
                default: return format.ToString();
            }
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