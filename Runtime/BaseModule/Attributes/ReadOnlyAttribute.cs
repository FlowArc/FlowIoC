using System;
using UnityEngine;

namespace FlowIoC.BaseModule.Attributes
{
    /// <summary>
    /// Shows a field in the Inspector without letting it be edited. A list or an array is locked with
    /// its size and its add and remove buttons, not only element by element, and a class or struct
    /// with every field inside it. Unity's list still lets an element be dragged to a new place, as
    /// on any disabled list.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ReadOnlyAttribute : PropertyAttribute
    {
        public ReadOnlyAttribute() : base(applyToCollection: true)
        {
        }
    }
    
#if UNITY_EDITOR
    
    [UnityEditor.CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyAttributeDrawer : UnityEditor.PropertyDrawer
    {
        public override float GetPropertyHeight(UnityEditor.SerializedProperty property, GUIContent label) =>
            UnityEditor.EditorGUI.GetPropertyHeight(property, label, true);

        public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
        {
            var wasEnabled = GUI.enabled;
            GUI.enabled = false;

            UnityEditor.EditorGUI.PropertyField(position, property, label, true);
            
            GUI.enabled = wasEnabled;
        }
    }
    
#endif
}