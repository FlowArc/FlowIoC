using System;
using System.Reflection;

namespace FlowIoC.BaseModule.Injectable.Utils
{
    /// <summary>
    /// One <c>[SignalParam]</c> property of a command, with the index written on it.
    /// Built once per command type and cached.
    /// </summary>
    internal readonly struct SignalParamEntry
    {
        public readonly PropertyInfo Property;
        public readonly Type Type;
        public readonly int Index;
        public readonly bool HasIndex;

        public SignalParamEntry(PropertyInfo property, int index, bool hasIndex)
        {
            Property = property;
            Type = property.PropertyType;
            Index = index;
            HasIndex = hasIndex;
        }
    }
}
