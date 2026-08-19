#if UNITY_EDITOR
using System;
using System.Reflection;
using FlowIoC.Editor.ModelViewer.PropertyDrawer.Properties;
using UnityEngine;

namespace FlowIoC.Editor.ModelViewer.MemberInfoDrawer.Properties
{
    internal class Vector3MemberInfoDrawer : MemberInfoDrawer<Vector3>
    {
        protected override Type _propertyDrawerType => typeof(Vector3PropertyDrawer);
        
        public Vector3MemberInfoDrawer(MemberInfo memberInfo, object targetObject) : base(memberInfo, targetObject)
        {
        }
    }
}
#endif