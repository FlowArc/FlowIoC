using System;

namespace FlowIoC.BaseModule.Attributes
{
    /// <summary>
    /// Declares what a component is, for the inspector header. Only needed when the type cannot
    /// be read on its own: a Root, a View or a screen is recognised without it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class FlowHeaderAttribute : Attribute
    {
        public FlowHeaderAttribute(FlowRole role, string title = null, string label = null)
        {
            Role = role;
            Title = title;
            Label = label;
        }

        public FlowRole Role { get; }

        /// <summary>Null when the header should use the type's own name.</summary>
        public string Title { get; }

        /// <summary>
        /// What the strip under the bar calls this type, when the role's own name would be a lie.
        /// A type can wear a role's colour without claiming to be one. Null uses the role name.
        /// </summary>
        public string Label { get; }
    }
}
