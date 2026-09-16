using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Function.ReturnableFunctions;
using FlowIoC.BaseModule.Signals;
using Modules.DeviceDebuggerModule.Data.ValueObjects;
using Modules.DeviceDebuggerModule.Entities;
using Modules.DeviceDebuggerModule.Enums;

namespace Modules.DeviceDebuggerModule.Controllers
{
    /// <summary>
    /// Every Incoming signal of every holder handed in, one row each, for the Signals tab. A row
    /// can fire when the signal carries nothing, or one payload the parser handles; the rest are
    /// drawn disabled with the payload named. The game's holders come first, the framework's last.
    /// </summary>
    internal class ScanSignalHoldersFunction : FunctionReturn<List<DebugSignalVO>, IReadOnlyList<ISignalHolder>>
    {
        private readonly SignalHolderWalker _walker = new();
        private readonly PayloadTypeRule _payloadRule = new();

        public override List<DebugSignalVO> Execute(IReadOnlyList<ISignalHolder> holders)
        {
            var rows = new List<DebugSignalVO>();

            if (holders == null) return rows;

            foreach (ISignalHolder holder in holders)
            {
                if (holder == null) continue;

                bool isFramework = _walker.IsFramework(holder.GetType());
                string holderName = holder.GetType().Name;

                foreach (SignalFieldVO field in _walker.Walk(holder))
                {
                    if (field.Half != SignalHalf.Incoming) continue;

                    rows.Add(Row(holderName, isFramework, field));
                }
            }

            rows.Sort(Compare);

            return rows;
        }

        private DebugSignalVO Row(string holderName, bool isFramework, SignalFieldVO field)
        {
            Type[] payloads = field.Signal.PayloadTypes ?? Type.EmptyTypes;
            var row = new DebugSignalVO
            {
                HolderName = holderName,
                SignalName = field.Name,
                IsFramework = isFramework,
                Signal = field.Signal,
                PayloadTypes = payloads
            };

            if (payloads.Length == 0 || (payloads.Length == 1 && _payloadRule.IsSupported(payloads[0])))
            {
                row.CanFire = true;
            }
            else
            {
                row.CanFire = false;
                row.Reason = "payload " + Names(payloads);
            }

            return row;
        }

        private static string Names(Type[] types)
        {
            var names = new string[types.Length];

            for (int i = 0; i < types.Length; i++) names[i] = types[i].Name;

            return string.Join(", ", names);
        }

        /// <summary>The game's holders first, the framework's last; by holder, then by signal.</summary>
        private static int Compare(DebugSignalVO a, DebugSignalVO b)
        {
            if (a.IsFramework != b.IsFramework) return a.IsFramework ? 1 : -1;

            int byHolder = string.CompareOrdinal(a.HolderName, b.HolderName);

            return byHolder != 0 ? byHolder : string.CompareOrdinal(a.SignalName, b.SignalName);
        }
    }
}
