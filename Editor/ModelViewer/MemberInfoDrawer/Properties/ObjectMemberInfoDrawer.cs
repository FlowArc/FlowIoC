#if UNITY_EDITOR
using System;
using System.Reflection;
using FlowIoC.Editor.ModelViewer.PropertyDrawer.Properties;
using Object = UnityEngine.Object;

namespace FlowIoC.Editor.ModelViewer.MemberInfoDrawer.Properties
{
    internal class ObjectMemberInfoDrawer : MemberInfoDrawer<Object>
    {
        protected override Type _propertyDrawerType => typeof(ObjectPropertyDrawer<Object>);
        
        public ObjectMemberInfoDrawer(MemberInfo memberInfo, object targetObject) : base(memberInfo, targetObject)
        {
        }
    }
}
#endif