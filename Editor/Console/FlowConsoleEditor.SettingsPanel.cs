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
    /// so they appear along the bar only while Settings is on.
    /// </summary>
    internal partial class FlowConsoleEditor
    {
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

        private void SettingsControlsGUI()
        {
            if (!_showSettings) return;

            RowLinesSettingGUI();
            SourceCaptureSettingGUI();
            ExportMenuGUI();
        }

        private void RowLinesSettingGUI()
        {
            var content = new GUIContent(_rowLineCount + " line" + (_rowLineCount == 1 ? "" : "s"),
                "How many lines a row shows. Two is Unity's shape: the message, and underneath it "
                + "where the message came from.");

            Rect rect = GUILayoutUtility.GetRect(content, EditorStyles.toolbarDropDown, GUILayout.Width(60f));

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
        private void SourceCaptureSettingGUI()
        {
            FlowStackTraceCapture capture = _settings.StackTraceCapture;

            var content = new GUIContent("Source: " + capture,
                "Which logs work out where they came from.");

            Rect rect = GUILayoutUtility.GetRect(content, EditorStyles.toolbarDropDown, GUILayout.Width(150f));

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