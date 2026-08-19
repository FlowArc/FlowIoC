using System;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.ScreenModule.ViewsMediators.Manager;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CustomEditorHeader
{
    [CustomEditor(typeof(ScreenManager))]
    public class ScreenManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            CustomHeaderDrawer.DrawHeaderFromAttribute(target);
            EditorGUILayout.Space(5);

            if (!Application.isPlaying)
            {
                var screenManager = (ScreenManager) target;
                var allScreenManagers = FindObjectsByType<ScreenManager>(FindObjectsSortMode.None);
                var sameIndexManagers = Array.FindAll(allScreenManagers, x => x.ManagerData.ManagerID == screenManager.ManagerData.ManagerID);

                if (sameIndexManagers.Length > 1)
                {
                    EditorGUILayout.HelpBox("There is too many ScreenManagers with same Index!!", MessageType.Error);
                }
            }

            base.OnInspectorGUI();
        }
    }

    [CustomEditor(typeof(Context))]
    public class ContextEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            CustomHeaderDrawer.DrawHeaderFromAttribute(target);
            EditorGUILayout.Space(5);
            base.OnInspectorGUI();
        }
    }

    // [CustomEditor(typeof(ScriptableRootAdapter))]
    // public class ScriptableRootAdapterEditor : UnityEditor.Editor
    // {
    //     public override void OnInspectorGUI()
    //     {
    //         CustomHeaderDrawer.DrawHeaderFromAttribute(target);
    //         EditorGUILayout.Space(5);
    //         base.OnInspectorGUI();
    //     }
    // }

    //[CustomEditor(typeof(PoolRootAdapter))]
    //public class PoolRootAdapterEditor : UnityEditor.Editor
    //{
    //    public override void OnInspectorGUI()
    //    {
    //        CustomHeaderDrawer.DrawHeaderFromAttribute(target);
    //        EditorGUILayout.Space(5);
    //        base.OnInspectorGUI();
    //   }
    //}
}