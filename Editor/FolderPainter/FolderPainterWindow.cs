#if UNITY_EDITOR

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
        [MenuItem("Tools/FlowIoC/Folder Painter", false, 150)]
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

            DrawToolbar(_serializedConfig.FindProperty("Enabled"));

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("PathRules"), _pathRulesLabel, true);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("FolderRules"), _folderRulesLabel, true);
            EditorGUILayout.EndScrollView();

            if (_serializedConfig.ApplyModifiedProperties()) RepaintFolders();
        }

        private void DrawToolbar(SerializedProperty enabled)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUI.BeginChangeCheck();
                bool value = GUILayout.Toggle(enabled.boolValue, "Enabled", EditorStyles.toolbarButton, GUILayout.Width(70));
                if (EditorGUI.EndChangeCheck()) enabled.boolValue = value;

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    RepaintFolders();
                }

                if (GUILayout.Button("Select Asset", EditorStyles.toolbarButton, GUILayout.Width(90)))
                {
                    Selection.activeObject = _config;
                    EditorGUIUtility.PingObject(_config);
                }
            }
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
