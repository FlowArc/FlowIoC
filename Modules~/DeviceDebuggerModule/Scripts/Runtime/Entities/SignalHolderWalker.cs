using System;
using System.Collections.Generic;
using System.Reflection;
using FlowIoC.BaseModule.Signals;
using Modules.DeviceDebuggerModule.Constants;
using Modules.DeviceDebuggerModule.Enums;

namespace Modules.DeviceDebuggerModule.Entities
{
    /// <summary>One signal field found on a holder, with the half it sits in.</summary>
    public class SignalFieldVO
    {
        public string Name = "";
        public SignalHalf Half;
        public ISignalBody Signal;
        public FieldInfo Field;
    }

    /// <summary>
    /// The walk both scans share. A holder's own signal fields count as Incoming - a flat holder
    /// has no halves. A field that is a plain object is a half, and its fields are the signals:
    /// Incoming when the field is named Incoming or its type name ends in it, Outgoing likewise,
    /// and anything else is Other, which the scans skip.
    /// </summary>
    public class SignalHolderWalker
    {
        private const BindingFlags FIELDS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public IEnumerable<SignalFieldVO> Walk(ISignalHolder holder)
        {
            if (holder == null) yield break;

            foreach (FieldInfo field in holder.GetType().GetFields(FIELDS))
            {
                object value = field.GetValue(holder);

                if (value == null) continue;

                if (value is ISignalBody signal)
                {
                    yield return new SignalFieldVO {Name = field.Name, Half = SignalHalf.Incoming, Signal = signal, Field = field};
                    continue;
                }

                Type type = value.GetType();

                if (!type.IsClass || type == typeof(string)) continue;

                SignalHalf half = HalfOf(field.Name, type.Name);

                foreach (FieldInfo inner in type.GetFields(FIELDS))
                {
                    if (inner.GetValue(value) is not ISignalBody innerSignal) continue;

                    yield return new SignalFieldVO {Name = inner.Name, Half = half, Signal = innerSignal, Field = inner};
                }
            }
        }

        public bool IsFramework(Type type) =>
            type != null && type.Assembly.GetName().Name == DeviceDebuggerConstants.FRAMEWORK_ASSEMBLY;

        /// <summary>The module a holder speaks for: its type name minus the Signals suffix.</summary>
        public string ModuleNameOf(Type holderType)
        {
            string name = holderType.Name;

            if (name.EndsWith("Signals", StringComparison.Ordinal) && name.Length > "Signals".Length)
                name = name.Substring(0, name.Length - "Signals".Length);

            return name;
        }

        private static SignalHalf HalfOf(string fieldName, string typeName)
        {
            if (fieldName == "Incoming" || typeName.EndsWith("Incoming", StringComparison.Ordinal)) return SignalHalf.Incoming;
            if (fieldName == "Outgoing" || typeName.EndsWith("Outgoing", StringComparison.Ordinal)) return SignalHalf.Outgoing;

            return SignalHalf.Other;
        }
    }
}
