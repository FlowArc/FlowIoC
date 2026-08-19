#if UNITY_EDITOR
using System;
using System.Reflection;
using FlowIoC.Editor.ModelViewer.PropertyDrawer.Properties;

namespace FlowIoC.Editor.ModelViewer.MemberInfoDrawer.Properties
{
    internal class EnumMemberInfoDrawer : MemberInfoDrawer<Enum>
    {
        protected override Type _propertyDrawerType => typeof(EnumPropertyDrawer);
        
        public EnumMemberInfoDrawer(MemberInfo memberInfo, object targetObject) : base(memberInfo, targetObject)
        {
        }
    }
}
#endif