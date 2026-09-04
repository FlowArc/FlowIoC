#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.CreateModule
{
    internal partial class CreateModuleMenu
    {
        private void DisplayActionsSection()
        {
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button(ADD_ACTION))
            {
                _actionNames.Add(NEW_ACTION);
            }

            GUI.backgroundColor = Color.white;

            _actionScrollPosition = EditorGUILayout.BeginScrollView(_actionScrollPosition, GUILayout.MaxHeight(60));

            // The row that is pressed is noted and dropped once the list has been drawn. Removing
            // it where the button is - and leaving the loop from inside its row, as this did -
            // ends the frame with a horizontal group still open, which IMGUI reports as a
            // mismatched layout group for every repaint that follows.
            int removeAt = -1;

            for (int ii = 0; ii < _actionNames.Count; ii++)
            {
                EditorGUILayout.BeginHorizontal();

                _actionNames[ii] = EditorGUILayout.TextField(_actionNames[ii]);

                GUI.backgroundColor = Color.red;

                if (GUILayout.Button("-", GUILayout.Width(30)))
                    removeAt = ii;

                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            if (removeAt >= 0)
                _actionNames.RemoveAt(removeAt);
        }
    }
}
#endif