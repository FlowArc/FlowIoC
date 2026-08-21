using System;
using System.Collections.Generic;
using System.Reflection;
using FlowIoC.BaseModule.Injectable.Attributes;

namespace FlowIoC.BaseModule.Injectable.Utils
{
    /// <summary>
    /// Collects the <c>[SignalParam]</c> properties of a type in a stable order:
    /// most-base class first, and within a class, source declaration order. The order
    /// matters because an unindexed property takes the next payload slot no other
    /// property has claimed.
    /// </summary>
    internal sealed class SignalParamEntryBuilder
    {
        private const BindingFlags DeclaredMembers =
            BindingFlags.DeclaredOnly | BindingFlags.Instance |
            BindingFlags.Public | BindingFlags.NonPublic;

        public List<SignalParamEntry> Build(Type targetType)
        {
            var entries = new List<SignalParamEntry>();
            if (targetType == null)
                return entries;

            var chain = new List<Type>();
            for (Type type = targetType; type != null && type != typeof(object); type = type.BaseType)
                chain.Add(type);
            chain.Reverse();

            var seenAccessors = new HashSet<MethodInfo>();

            foreach (Type type in chain)
            {
                PropertyInfo[] properties = type.GetProperties(DeclaredMembers);
                Array.Sort(properties, CompareByMetadataToken);

                foreach (PropertyInfo property in properties)
                {
                    var attribute = property.GetCustomAttribute<SignalParamAttribute>(false);
                    if (attribute == null)
                        continue;

                    // An override re-declares a property the base class already gave us.
                    // Record it once, at the declaration that carries the attribute.
                    MethodInfo accessor = property.GetMethod ?? property.SetMethod;
                    MethodInfo declaration = accessor?.GetBaseDefinition() ?? accessor;
                    if (declaration != null && !seenAccessors.Add(declaration))
                        continue;

                    entries.Add(new SignalParamEntry(property, attribute.Index, attribute.HasIndex));
                }
            }

            return entries;
        }

        private int CompareByMetadataToken(PropertyInfo left, PropertyInfo right)
            => left.MetadataToken.CompareTo(right.MetadataToken);
    }
}
