using System;
using System.Collections.Generic;

namespace FlowIoC.BaseModule.Controller
{
    /// <summary>
    /// A Command's name as a reader knows it. A step a Service ships is nested in the Service's
    /// interface - <c>IHapticService.Commands.Play</c> - and <c>Type.Name</c> answers <c>Play</c>,
    /// which names nothing once two Services ship one. The declaring chain is walked back to the
    /// outermost type and the namespace is left off, so the line reads the way the binding was
    /// written: <c>IHapticService.Commands.Play</c>. A type that is not nested is its Name.
    /// </summary>
    public class CommandDisplayName
    {
        public string Of(Type type)
        {
            if (type == null)
                return string.Empty;

            if (type.DeclaringType == null)
                return type.Name;

            var parts = new List<string>();

            for (Type current = type; current != null; current = current.DeclaringType)
                parts.Add(current.Name);

            parts.Reverse();
            return string.Join(".", parts);
        }
    }
}
