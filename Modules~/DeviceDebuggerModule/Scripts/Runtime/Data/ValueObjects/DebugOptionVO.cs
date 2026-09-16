using System;
using FlowIoC.BaseModule.Signals;
using Modules.DeviceDebuggerModule.Constants;
using Modules.DeviceDebuggerModule.Enums;

namespace Modules.DeviceDebuggerModule.Data.ValueObjects
{
    /// <summary>
    /// One row of the Options tab, as the scan found it: either an annotated signal field, or an
    /// annotated step with the debugger's own trigger signal bound to it. A Value row also keeps
    /// what its signal last carried.
    /// </summary>
    public class DebugOptionVO
    {
        public string Category = "";
        public string Label = "";
        public int Order;
        public DebugOptionKind Kind;

        /// <summary>The annotated field's signal; null for a step.</summary>
        public ISignalBody Signal;

        /// <summary>The annotated step; null for a field.</summary>
        public Type CommandType;

        /// <summary>The debugger's own signal bound to the step, set at discovery.</summary>
        public Signal Trigger;

        /// <summary>The attribute's constant: a button's payload, or a step's bound parameter.</summary>
        public object Argument;

        public double Min = double.NaN;
        public double Max = double.NaN;
        public double Step = double.NaN;

        public bool HasRange => !double.IsNaN(Min) && !double.IsNaN(Max);

        /// <summary>The one payload a control edits; null for a button and a step.</summary>
        public Type PayloadType;

        /// <summary>An enum's names, for a Choice.</summary>
        public string[] Choices;

        /// <summary>Why the row is Unsupported.</summary>
        public string Reason = "";

        public string LastValue = DeviceDebuggerConstants.NO_VALUE;
        public int Dispatches;

        public string Key => Category + "/" + Label;
    }
}
