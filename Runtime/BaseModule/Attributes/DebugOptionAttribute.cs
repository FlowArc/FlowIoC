using System;

namespace FlowIoC.BaseModule.Attributes
{
    /// <summary>
    /// Offers a signal, or a step a Service ships, on the on-device debug panel. On a field of a
    /// public signal holder: an <c>Incoming</c> signal becomes a control - a button for a
    /// <c>Signal</c>, a toggle for a <c>Signal&lt;bool&gt;</c>, a number for an int, float or double,
    /// a text field for a string, a choice for an enum - and an <c>Outgoing</c> signal becomes a
    /// value row showing the last payload it carried. On a class deriving from <c>Command</c>: a
    /// button that runs the step, with <see cref="Argument"/> as its bound parameter. A step may
    /// carry the attribute more than once, one row per argument: Play Success, Play Failure.
    ///
    /// It lives in the package rather than in the debugger module so a holder compiles whether or
    /// not the debugger is installed, and so a ready-made module can annotate a step it ships
    /// without referencing anything.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class, AllowMultiple = true)]
    public class DebugOptionAttribute : Attribute
    {
        /// <summary>The group the row sits in. Empty: the module the holder or the step belongs to.</summary>
        public string Category = "";

        /// <summary>The row's text. Empty: the field or class name, spaced at each capital.</summary>
        public string Label = "";

        /// <summary>Rows in a category sort by this, then by label.</summary>
        public int Order;

        /// <summary>
        /// A constant handed to the signal or the step when the button is pressed: the payload of
        /// a <c>Signal&lt;T&gt;</c> offered as a button, or the parameter of a <c>Command&lt;T&gt;</c>.
        /// The attribute language allows a number, a string, a bool, an enum or a type.
        /// </summary>
        public object Argument;

        /// <summary>The lower end of a number's slider. Both ends set make the row a slider.</summary>
        public double Min = double.NaN;

        /// <summary>The upper end of a number's slider.</summary>
        public double Max = double.NaN;

        /// <summary>The slider's step. Not set: continuous for a float, one for an int.</summary>
        public double Step = double.NaN;

        public bool HasRange => !double.IsNaN(Min) && !double.IsNaN(Max);

        public DebugOptionAttribute()
        {
        }

        public DebugOptionAttribute(string category, string label)
        {
            Category = category ?? "";
            Label = label ?? "";
        }
    }
}
