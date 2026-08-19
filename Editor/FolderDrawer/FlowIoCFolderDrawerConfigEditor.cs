#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.FolderDrawer
{
    [CustomEditor(typeof(FlowIoCFolderDrawerConfig))]
    public class FlowIoCFolderDrawerConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();

            if (GUILayout.Button("Refresh", GUILayout.Height(40)))
            {
                FlowIoCFolderDrawerBootstrap.Drawer.Apply();
                EditorApplication.RepaintProjectWindow();
            }

            if (GUILayout.Button("Open Folder Drawer Window"))
            {
                FlowIoCFolderDrawerWindow.Open();
            }
        }
    }
}
#endif
