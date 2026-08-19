#if UNITY_EDITOR
using System;
using System.Reflection;
using FlowIoC.Editor.ModelViewer.PropertyDrawer.Properties;

namespace FlowIoC.Editor.ModelViewer.MemberInfoDrawer.Properties
{
    internal class FloatMemberInfoDrawer : MemberInfoDrawer<float>
    {
        protected override Type _propertyDrawerType => typeof(FloatPropertyDrawer);
        
        public FloatMemberInfoDrawer(MemberInfo memberInfo, object targetObject) : base(memberInfo, targetObject)
        {
        }
    }
}
#endif