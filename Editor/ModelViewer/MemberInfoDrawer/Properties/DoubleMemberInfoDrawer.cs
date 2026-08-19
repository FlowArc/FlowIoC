#if UNITY_EDITOR
using System;
using System.Reflection;
using FlowIoC.Editor.ModelViewer.PropertyDrawer.Properties;

namespace FlowIoC.Editor.ModelViewer.MemberInfoDrawer.Properties
{
    internal class DoubleMemberInfoDrawer : MemberInfoDrawer<double>
    {
        protected override Type _propertyDrawerType => typeof(DoublePropertyDrawer);
        public DoubleMemberInfoDrawer(MemberInfo memberInfo, object targetObject) : base(memberInfo, targetObject)
        {
        }
    }
}
#endif