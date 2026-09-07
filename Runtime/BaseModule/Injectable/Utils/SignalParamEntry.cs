using System;
using System.Reflection;

namespace FlowIoC.BaseModule.Injectable.Utils
{
    /// <summary>
    /// One <c>[SignalParam]</c> property of a command, with the index written on it and its setter
    /// ready to call. Built once per command type and cached.
    /// </summary>
    internal readonly struct SignalParamEntry
    {
        public readonly PropertyInfo Property;
        public readonly Type Type;
        public readonly int Index;
        public readonly bool HasIndex;

        /// <summary>
        /// Writes the property without going through reflection on each execution. Null when the
        /// property has no setter, which the builder reports and leaves out.
        /// </summary>
        public readonly Action<object, object> Set;

        public SignalParamEntry(PropertyInfo property, int index, bool hasIndex)
        {
            Property = property;
            Type = property.PropertyType;
            Index = index;
            HasIndex = hasIndex;
            Set = PropertySetter.For(property);
        }
    }
}
