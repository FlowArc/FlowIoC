using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CustomEditorHeader
{
    [CustomEditor(typeof(MonoBehaviour), true)]
    public class GenericCustomHeaderEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            CustomHeaderDrawer.DrawHeaderFromAttribute(target);
            EditorGUILayout.Space(5);
            base.OnInspectorGUI();
        }
    }

    [CustomEditor(typeof(ScriptableObject), true)]
    public class GenericScriptableObjectHeaderEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            CustomHeaderDrawer.DrawHeaderFromAttribute(target);
            EditorGUILayout.Space(5);
            base.OnInspectorGUI();
        }
    }
} 