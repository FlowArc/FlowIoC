#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.FolderPainter
{
    [CustomEditor(typeof(ED_FolderPainter))]
    public class FolderPainterConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();

            if (GUILayout.Button("Refresh", GUILayout.Height(40)))
            {
                FolderPainterBootstrap.Painter.Apply();
                EditorApplication.RepaintProjectWindow();
            }

            if (GUILayout.Button("Open Folder Painter Window"))
            {
                FolderPainterWindow.Open();
            }
        }
    }
}
#endif
