using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Function.ReturnableFunctions;
using FlowIoC.BaseModule.Signals;
using Modules.DeviceDebuggerModule.Data.ValueObjects;
using Modules.DeviceDebuggerModule.Entities;
using Modules.DeviceDebuggerModule.Enums;

namespace Modules.DeviceDebuggerModule.Controllers
{
    /// <summary>
    /// The Options tab's rows: every [DebugOption] field on the holders handed in, and every
    /// [DebugOption] command type. An Incoming field becomes a control by its payload type, an
    /// Outgoing field a Value row, a command type a button. Category and label default to the
    /// module and the spaced name; rows sort by category, order, label.
    /// </summary>
    internal class ScanDebugOptionsFunction : FunctionReturn<List<DebugOptionVO>, IReadOnlyList<ISignalHolder>, IReadOnlyList<Type>>
    {
        private readonly SignalHolderWalker _walker = new();
        private readonly PayloadTypeRule _payloadRule = new();

        public override List<DebugOptionVO> Execute(IReadOnlyList<ISignalHolder> holders, IReadOnlyList<Type> commandTypes)
        {
            var rows = new List<DebugOptionVO>();

            if (holders != null)
            {
                foreach (ISignalHolder holder in holders)
                {
                    if (holder == null) continue;

                    string module = _walker.ModuleNameOf(holder.GetType());

                    foreach (SignalFieldVO field in _walker.Walk(holder))
                    {
                        if (field.Half == SignalHalf.Other) continue;

                        foreach (DebugOptionAttribute attribute in field.Field.GetCustomAttributes<DebugOptionAttribute>())
                            rows.Add(FromField(field, attribute, module));
                    }
                }
            }

            if (commandTypes != null)
            {
                foreach (Type type in commandTypes)
                {
                    if (type == null) continue;

                    // One row per attribute: a step bound with a different argument each time.
                    foreach (DebugOptionAttribute attribute in type.GetCustomAttributes<DebugOptionAttribute>())
                        rows.Add(FromCommand(type, attribute));
                }
            }

            rows.Sort(Compare);

            return rows;
        }

        private DebugOptionVO FromField(SignalFieldVO field, DebugOptionAttribute attribute, string module)
        {
            DebugOptionVO row = Base(attribute, module, field.Name);
            row.Signal = field.Signal;

            Type[] payloads = field.Signal.PayloadTypes ?? Type.EmptyTypes;

            if (field.Half == SignalHalf.Outgoing)
            {
                row.Kind = DebugOptionKind.Value;
                return row;
            }

            if (payloads.Length == 0)
            {
                row.Kind = DebugOptionKind.Button;
                return row;
            }

            if (payloads.Length > 1)
            {
                Unsupported(row, "payload " + Names(payloads) + " - the panel edits one payload");
                return row;
            }

            Type payload = payloads[0];
            row.PayloadType = payload;

            if (row.Argument != null)
            {
                // A button that fires the signal with the constant, whatever the payload type is,
                // as long as the constant is of that type.
                if (payload.IsInstanceOfType(row.Argument) || (payload.IsEnum && row.Argument.GetType() == Enum.GetUnderlyingType(payload)))
                {
                    row.Kind = DebugOptionKind.Button;
                    if (payload.IsEnum && !payload.IsInstanceOfType(row.Argument)) row.Argument = Enum.ToObject(payload, row.Argument);
                    return row;
                }

                Unsupported(row, "Argument is a " + row.Argument.GetType().Name + ", the payload a " + payload.Name);
                return row;
            }

            if (payload == typeof(bool)) row.Kind = DebugOptionKind.Toggle;
            else if (_payloadRule.IsNumber(payload)) row.Kind = DebugOptionKind.Number;
            else if (payload == typeof(string)) row.Kind = DebugOptionKind.Text;
            else if (payload.IsEnum)
            {
                row.Kind = DebugOptionKind.Choice;
                row.Choices = Enum.GetNames(payload);
            }
            else Unsupported(row, "payload " + payload.Name + " cannot be typed on the panel");

            return row;
        }

        private DebugOptionVO FromCommand(Type type, DebugOptionAttribute attribute)
        {
            DebugOptionVO row = Base(attribute, ModuleNameOf(type), type.Name);
            row.CommandType = type;
            row.Kind = DebugOptionKind.Button;

            return row;
        }

        private static DebugOptionVO Base(DebugOptionAttribute attribute, string module, string memberName)
        {
            return new DebugOptionVO
            {
                Category = string.IsNullOrWhiteSpace(attribute.Category) ? module : attribute.Category.Trim(),
                Label = string.IsNullOrWhiteSpace(attribute.Label) ? Spaced(memberName) : attribute.Label.Trim(),
                Order = attribute.Order,
                Argument = attribute.Argument,
                Min = attribute.Min,
                Max = attribute.Max,
                Step = attribute.Step
            };
        }

        private static void Unsupported(DebugOptionVO row, string reason)
        {
            row.Kind = DebugOptionKind.Unsupported;
            row.Reason = reason;
        }

        /// <summary>The module a step belongs to: the assembly's last name segment after Modules., else the assembly.</summary>
        private static string ModuleNameOf(Type type)
        {
            string assembly = type.Assembly.GetName().Name ?? "";
            const string prefix = "Modules.";

            if (assembly.StartsWith(prefix, StringComparison.Ordinal) && assembly.Length > prefix.Length)
            {
                string rest = assembly.Substring(prefix.Length);
                int dot = rest.IndexOf('.');

                return dot > 0 ? rest.Substring(0, dot) : rest;
            }

            return assembly;
        }

        /// <summary>WinLevel to "Win Level"; a Command suffix dropped from a step's class name.</summary>
        private static string Spaced(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";

            if (name.EndsWith("Command", StringComparison.Ordinal) && name.Length > "Command".Length)
                name = name.Substring(0, name.Length - "Command".Length);

            var text = new StringBuilder(name.Length + 4);

            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];

                if (i > 0 && char.IsUpper(c) && !char.IsUpper(name[i - 1]) && name[i - 1] != ' ')
                    text.Append(' ');

                text.Append(c);
            }

            return text.ToString();
        }

        private static string Names(Type[] types)
        {
            var names = new string[types.Length];

            for (int i = 0; i < types.Length; i++) names[i] = types[i].Name;

            return string.Join(", ", names);
        }

        private static int Compare(DebugOptionVO a, DebugOptionVO b)
        {
            int byCategory = string.CompareOrdinal(a.Category, b.Category);
            if (byCategory != 0) return byCategory;

            int byOrder = a.Order.CompareTo(b.Order);

            return byOrder != 0 ? byOrder : string.CompareOrdinal(a.Label, b.Label);
        }
    }
}
