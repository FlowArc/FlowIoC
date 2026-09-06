#if UNITY_EDITOR

using FlowIoC.Editor.Inspector;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.FolderPainter
{
    /// <summary>
    /// Edits the project local folder painter config. Every change is written straight to
    /// the asset and repainted in the Project window, so the colors update as you type.
    /// </summary>
    internal class FolderPainterWindow : EditorWindow
    {
        [MenuItem("Tools/FlowIoC/Folder Painter", false, -1150)]
        internal static void Open()
        {
            FolderPainterWindow window = GetWindow<FolderPainterWindow>("Folder Painter");
            window.minSize = new Vector2(380, 320);
            window.Show();
        }

        private readonly GUIContent _pathRulesLabel = new GUIContent(
            "Path Rules",
            "Checked in order against every folder path. The first match wins.");

        private readonly GUIContent _folderRulesLabel = new GUIContent(
            "Folder Rules",
            "Colors one specific folder asset. Takes priority over the path rules.");

        private readonly FlowRowPainter _painter = new FlowRowPainter();
        private readonly FlowHeaderBar _bar = new FlowHeaderBar(new FlowPalette(), new FlowHelpPageMap());

        private ED_FolderPainter _config;
        private SerializedObject _serializedConfig;
        private Vector2 _scroll;

        private void OnEnable()
        {
            BindConfig();
        }

        private void OnFocus()
        {
            // the asset can be deleted or reimported from the Project window while this is open
            if (_config == null) BindConfig();
        }

        private void OnLostFocus()
        {
            if (_config != null) AssetDatabase.SaveAssetIfDirty(_config);
        }

        private void BindConfig()
        {
            _config = FolderPainterBootstrap.Painter.EnsureConfig();
            _serializedConfig = _config == null ? null : new SerializedObject(_config);
        }

        private void OnGUI()
        {
            if (_config == null || _serializedConfig == null)
            {
                DrawMissingConfig();
                return;
            }

            _serializedConfig.Update();

            _bar.DrawWindow("Folder Painter", "FlowIoC", "Colours the Project window's folders",
                "Refresh", RepaintFolders, "Folder Painter", "Select Asset", SelectConfig);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("PathRules"), _pathRulesLabel, true);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("FolderRules"), _folderRulesLabel, true);
            EditorGUILayout.EndScrollView();

            DrawEnabled(_serializedConfig.FindProperty("Enabled"));

            if (_serializedConfig.ApplyModifiedProperties()) RepaintFolders();
        }

        /// <summary>
        /// The switch, under the rules it governs, where the other windows keep their action. It is
        /// a toggle rather than an action, so it says which way it is set rather than what pressing
        /// it would do: a tick and the row green while the painter is on, a warning and the amber
        /// while it is off - because rules that are written and not applied is the state worth
        /// noticing.
        /// </summary>
        private void DrawEnabled(SerializedProperty enabled)
        {
            bool on = enabled.boolValue;
            var content = new GUIContent(
                on ? " Enabled" : " Disabled",
                EditorGUIUtility.IconContent(on ? "TestPassed" : "console.warnicon.sml").image,
                on
                    ? "The folder colours are being applied. Click to stop painting."
                    : "The rules below are not being applied. Click to start painting.");

            Color background = GUI.backgroundColor;
            GUI.backgroundColor = on ? _painter.Ok : _painter.Warn;

            EditorGUI.BeginChangeCheck();
            bool value = GUILayout.Toggle(on, content, _painter.ActionButton, GUILayout.ExpandWidth(true));
            if (EditorGUI.EndChangeCheck()) enabled.boolValue = value;

            GUI.backgroundColor = background;
        }

        private void SelectConfig()
        {
            Selection.activeObject = _config;
            EditorGUIUtility.PingObject(_config);
        }

        private void DrawMissingConfig()
        {
            EditorGUILayout.HelpBox(
                $"No config found at {FolderPainterBootstrap.Painter.ConfigPath}.",
                MessageType.Warning);

            if (GUILayout.Button("Create Config")) BindConfig();
        }

        private void RepaintFolders()
        {
            FolderPainterBootstrap.Painter.Apply();
            EditorApplication.RepaintProjectWindow();
        }
    }
}
#endif