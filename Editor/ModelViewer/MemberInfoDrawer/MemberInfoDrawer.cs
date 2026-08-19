#if UNITY_EDITOR
using System;
using System.Reflection;
using FlowIoC.Editor.ModelViewer.PropertyDrawer;

namespace FlowIoC.Editor.ModelViewer.MemberInfoDrawer
{
    internal class MemberInfoDrawer<TPropertyType> : MemberInfoDrawerBase
    {
        public TPropertyType PropertyType { get; set; }

        protected MemberInfoDrawer(MemberInfo memberInfo, object targetObject) : base(memberInfo, targetObject)
        {
        }

        protected override void CreatePropertyDrawer()
        {
            if (_propertyDrawerType == null)
                return;

            _propertyDrawer = (PropertyDrawerBase)Activator.CreateInstance(_propertyDrawerType, _fieldName, _hasPropertyReadOnly);

            ((PropertyDrawer<TPropertyType>)_propertyDrawer).SetValue(GetPropertyValue());
            _propertyDrawer.OnValueChanged += () => { SetValue(((PropertyDrawer<TPropertyType>)_propertyDrawer).GetValue()); };
        }

        private TPropertyType GetPropertyValue()
        {
            return (TPropertyType)_memberInfo.GetValue(_targetObject);
        }

        private void SetValue(TPropertyType newValue)
        {
            _memberInfo.SetValue(_targetObject, newValue);
        }

        protected override void OnDrawGUI()
        {
            ((PropertyDrawer<TPropertyType>)_propertyDrawer).SetValue(GetPropertyValue());
            base.OnDrawGUI();
        }
    }
}
#endif