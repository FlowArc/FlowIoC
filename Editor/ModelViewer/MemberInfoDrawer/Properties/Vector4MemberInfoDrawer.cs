#if UNITY_EDITOR
using System;
using System.Reflection;
using FlowIoC.Editor.ModelViewer.PropertyDrawer.Properties;
using UnityEngine;

namespace FlowIoC.Editor.ModelViewer.MemberInfoDrawer.Properties
{
    internal class Vector4MemberInfoDrawer : MemberInfoDrawer<Vector4>
    {
        protected override Type _propertyDrawerType => typeof(Vector4PropertyDrawer);
        
        public Vector4MemberInfoDrawer(MemberInfo memberInfo, object targetObject) : base(memberInfo, targetObject)
        {
        }
    }
}
#endif