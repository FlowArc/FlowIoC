#if UNITY_EDITOR
using System;
using System.Reflection;
using FlowIoC.Editor.ModelViewer.PropertyDrawer.Properties;

namespace FlowIoC.Editor.ModelViewer.MemberInfoDrawer.Properties
{
    internal class BoolMemberInfoDrawer : MemberInfoDrawer<bool>
    {
        protected override Type _propertyDrawerType => typeof(BoolPropertyDrawer);

        public BoolMemberInfoDrawer(MemberInfo memberInfo, object targetObject) : base(memberInfo, targetObject)
        {
        }
    }
}
#endif