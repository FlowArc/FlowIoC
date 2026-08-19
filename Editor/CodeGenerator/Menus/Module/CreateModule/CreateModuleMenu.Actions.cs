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
            for (int i = 0; i < _actionNames.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                _actionNames[i] = EditorGUILayout.TextField(_actionNames[i]);

                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("-", GUILayout.Width(30)))
                {
                    _actionNames.RemoveAt(i);
                    break;
                }

                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
#endif