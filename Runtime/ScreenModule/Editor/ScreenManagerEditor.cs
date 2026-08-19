#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using FlowIoC.ScreenModule.ViewsMediators.Manager;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.ScreenModule.Editor
{
    [CustomEditor(typeof(ScreenManager), true)]
    [CanEditMultipleObjects]
    public class ScreenManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
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

            base.OnInspectorGUI();
        }
    }
}
#endif