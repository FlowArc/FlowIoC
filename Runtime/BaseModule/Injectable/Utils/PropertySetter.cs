using System;
using System.Reflection;

namespace FlowIoC.BaseModule.Injectable.Utils
{
    /// <summary>
    /// A property's setter as a delegate that takes the target and the value as objects. Built
    /// once per property and kept on the entry that describes it: <c>PropertyInfo.SetValue</c>
    /// goes through reflection on every call, and a <c>[SignalParam]</c> property is filled on
    /// every execution of its command. The typed setter delegate sits inside a generic box, so a
    /// call is one interface call, one delegate call and two casts.
    ///
    /// Where the runtime cannot build the box - an ahead-of-time build that lacks the generic
    /// instantiation, or a property declared on a struct - the reflective setter stands in,
    /// slower but correct.
    /// </summary>
    internal static class PropertySetter
    {
        /// <summary>Null when the property has no setter; nothing can be written through it.</summary>
        public static Action<object, object> For(PropertyInfo property)
        {
            MethodInfo setMethod = property.SetMethod;
            if (setMethod == null)
                return null;

            if (property.DeclaringType == null || property.DeclaringType.IsValueType)
                return Reflective(property);

            try
            {
                Type boxType = typeof(Box<,>).MakeGenericType(property.DeclaringType, property.PropertyType);
                ISetter box = (ISetter) Activator.CreateInstance(boxType, setMethod);
                return box.Set;
            }
            catch (Exception)
            {
                return Reflective(property);
            }
        }

        private static Action<object, object> Reflective(PropertyInfo property)
            => (target, value) => property.SetValue(target, value);

        private interface ISetter
        {
            void Set(object target, object value);
        }

        private sealed class Box<TTarget, TValue> : ISetter
        {
            private readonly Action<TTarget, TValue> _set;

            public Box(MethodInfo setMethod)
            {
                _set = (Action<TTarget, TValue>) Delegate.CreateDelegate(typeof(Action<TTarget, TValue>), setMethod);
            }

            public void Set(object target, object value) => _set((TTarget) target, (TValue) value);
        }
    }
}
