#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.FolderDrawer
{
    [CustomEditor(typeof(ED_FolderDrawer))]
    public class FolderDrawerConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();

            if (GUILayout.Button("Refresh", GUILayout.Height(40)))
            {
                FolderDrawerBootstrap.Drawer.Apply();
                EditorApplication.RepaintProjectWindow();
            }

            if (GUILayout.Button("Open Folder Drawer Window"))
            {
                FolderDrawerWindow.Open();
            }
        }
    }
}
#endif
