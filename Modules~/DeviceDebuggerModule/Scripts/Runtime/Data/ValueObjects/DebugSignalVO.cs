using System;
using FlowIoC.BaseModule.Signals;

namespace Modules.DeviceDebuggerModule.Data.ValueObjects
{
    /// <summary>One row of the Signals tab: an Incoming signal of a cross-context holder.</summary>
    public class DebugSignalVO
    {
        public string HolderName = "";
        public string SignalName = "";
        public bool IsFramework;
        public ISignalBody Signal;
        public Type[] PayloadTypes = Type.EmptyTypes;

        /// <summary>No payload, or one the parser handles.</summary>
        public bool CanFire;

        public string Reason = "";
    }
}
