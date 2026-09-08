#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.Console
{
    /// <summary>
    /// Asks what to call a preset. A modal window rather than a text field in the toolbar, because
    /// naming one is something a reader does twice a month and the toolbar is read every minute.
    /// </summary>
    internal class FlowConsolePresetNameWindow : EditorWindow
    {
        private string _name = "";
        private Action<string> _onSave;

        internal static void Show(Action<string> onSave)
        {
            var window = CreateInstance<FlowConsolePresetNameWindow>();
            window.titleContent = new GUIContent("Save Filter Preset");
            window._onSave = onSave;
            window.minSize = new Vector2(320f, 90f);
            window.maxSize = new Vector2(320f, 90f);
            window.ShowModalUtility();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Name this set of channels.", EditorStyles.miniLabel);

            GUI.SetNextControlName("FlowConsolePresetName");
            _name = EditorGUILayout.TextField(_name);
            EditorGUI.FocusTextInControl("FlowConsolePresetName");

            EditorGUILayout.Space(6f);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Cancel", GUILayout.Width(80f)))
                Close();

            EditorGUI.BeginDisabledGroup(string.IsNullOrWhiteSpace(_name));

            bool entered = Event.current.type == EventType.KeyDown &&
                           (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter);

            if (GUILayout.Button("Save", GUILayout.Width(80f)) || (entered && !string.IsNullOrWhiteSpace(_name)))
            {
                _onSave?.Invoke(_name.Trim());
                Close();
            }

            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
