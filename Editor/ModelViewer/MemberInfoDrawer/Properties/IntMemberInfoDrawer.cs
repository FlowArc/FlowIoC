#if UNITY_EDITOR
using System;
using System.Reflection;
using FlowIoC.Editor.ModelViewer.PropertyDrawer.Properties;

namespace FlowIoC.Editor.ModelViewer.MemberInfoDrawer.Properties
{
    internal class IntMemberInfoDrawer : MemberInfoDrawer<int>
    {
        protected override Type _propertyDrawerType => typeof(IntPropertyDrawer);
        
        public IntMemberInfoDrawer(MemberInfo memberInfo, object targetObject) : base(memberInfo, targetObject)
        {
        }
    }
}
#endif