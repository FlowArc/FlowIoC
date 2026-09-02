#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using FlowIoC.ScreenModule.ViewsMediators.Manager;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.Inspector
{
    /// <summary>
    /// The screen manager's own inspector. It lives in the Editor assembly rather than beside the
    /// manager, because a dedicated editor is the only place a component with one can get the
    /// FlowIoC bar.
    /// </summary>
    [CustomEditor(typeof(ScreenManager), true)]
    [CanEditMultipleObjects]
    public class ScreenManagerEditor : UnityEditor.Editor
    {
        private FlowComponentBar _bar;

        private void OnEnable()
        {
            _bar = new FlowComponentBar();
        }

        public override void OnInspectorGUI()
        {
            _bar.Draw(target != null ? target.GetType() : null);

            if (!Application.isPlaying)
            {
                List<ScreenManager> allScreenManagers = FindObjectsByType<ScreenManager>(FindObjectsSortMode.None).ToList();
                foreach (ScreenManager screenManager in allScreenManagers)
                {
                    int managerIndex = screenManager.ManagerData.ManagerID;
                    List<ScreenManager> sameIndexManagers = allScreenManagers
                        .Where(x => x.ManagerData.ManagerID == managerIndex)
                        .ToList();

                    if (sameIndexManagers.Count == 1) continue;
                    EditorGUILayout.HelpBox("There is too many ScreenManagers with same Index!!",
                        MessageType.Error);
                    break;
                }
            }

            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
