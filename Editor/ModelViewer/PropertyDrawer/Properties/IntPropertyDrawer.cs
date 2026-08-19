#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.ModelViewer.PropertyDrawer.Properties
{
    internal class IntPropertyDrawer : PropertyDrawer<int>
    {
        public IntPropertyDrawer(string fieldName, bool readOnly) : base(fieldName, readOnly)
        {
        }

        protected override void OnDrawGUI()
        {
            base.OnDrawGUI();
            
            var propertyValue = GetValue();
            int newValue;
            if(ShowFieldName)
                newValue = EditorGUILayout.IntField(new GUIContent(_fieldName), propertyValue);
            else
                newValue = EditorGUILayout.IntField(propertyValue);
            
            if(newValue != propertyValue)
                SetValue(newValue);
        }
    }
}
#endif