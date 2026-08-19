#if UNITY_EDITOR
using System;
using System.Reflection;
using FlowIoC.Editor.ModelViewer.PropertyDrawer.Properties;

namespace FlowIoC.Editor.ModelViewer.MemberInfoDrawer.Properties
{
    
    internal class GenericMemberInfoDrawer<T> : MemberInfoDrawer<T>
    {
        protected override Type _propertyDrawerType => typeof(GenericTypeDrawer<T>);

        public GenericMemberInfoDrawer(MemberInfo memberInfo, object targetObject) : base(memberInfo, targetObject)
        {
        }
    }
}
#endif 