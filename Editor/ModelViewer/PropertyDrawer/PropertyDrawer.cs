#if UNITY_EDITOR
using System;

namespace FlowIoC.Editor.ModelViewer.PropertyDrawer
{
    internal class PropertyDrawer<TPropertyType> : PropertyDrawerBase, IDisposable
    {
        public TPropertyType PropertyType { get; set; }

        private TPropertyType _property;

        protected PropertyDrawer(string fieldName, bool readOnly) : base(fieldName, readOnly)
        {
        }

        public TPropertyType GetValue()
        {
            return _property;
        }

        public void SetValue(TPropertyType newValue)
        {
            _property = newValue;
            OnValueChanged?.Invoke();
        }

        public void Dispose()
        {
        }
    }
}
#endif