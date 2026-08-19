#if UNITY_EDITOR
using System;
using System.Reflection;
using FlowIoC.Editor.ModelViewer.PropertyDrawer.Properties;

namespace FlowIoC.Editor.ModelViewer.MemberInfoDrawer.Properties
{
    internal class StringMemberInfoDrawer : MemberInfoDrawer<string>
    {
        protected override Type _propertyDrawerType => typeof(StringPropertyDrawer);
        
        public StringMemberInfoDrawer(MemberInfo memberInfo, object targetObject) : base(memberInfo, targetObject)
        {
        }
    }
}
#endif