#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using FlowIoC.Editor.ModelViewer.PropertyDrawer.Properties;
using UnityEditor;

namespace FlowIoC.Editor.ModelViewer.MemberInfoDrawer.Properties
{
    internal class ListMemberInfoDrawer<T> : MemberInfoDrawer<List<T>>
    {
        protected override Type _propertyDrawerType => typeof(ListPropertyDrawer<T>);

        public ListMemberInfoDrawer(MemberInfo memberInfo, object targetObject) : base(memberInfo, targetObject)
        {
        }

        protected override void OnDrawGUI()
        {
            EditorGUILayout.BeginVertical("box");
            base.OnDrawGUI();
            EditorGUILayout.EndVertical();
        }
    }
}
#endif