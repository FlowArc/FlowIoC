using System;
using System.Collections.Generic;

namespace FlowIoC.BaseModule.Controller
{
    /// <summary>
    /// A Command's name as a reader knows it. A step a Service ships is nested in the Service's
    /// interface - <c>IHapticService.Commands.Play</c> - and <c>Type.Name</c> answers <c>Play</c>,
    /// which names nothing once two Services ship one. The declaring chain is walked back to the
    /// outermost type and the namespace is left off, so the line reads the way the binding was
    /// written: <c>IHapticService.Commands.Play</c>. A generic step reads with its arguments the
    /// same way - <c>IAssetService.Commands.LoadGroupByLabel&lt;Sprite&gt;</c> rather than the
    /// compiler's <c>LoadGroupByLabel`1</c>. A type that is not nested is its Name.
    /// </summary>
    public class CommandDisplayName
    {
        public string Of(Type type)
        {
            if (type == null)
                return string.Empty;

            if (type.DeclaringType == null)
                return NameOf(type);

            var parts = new List<string>();

            for (Type current = type; current != null; current = current.DeclaringType)
                parts.Add(NameOf(current));

            parts.Reverse();
            return string.Join(".", parts);
        }

        private string NameOf(Type type)
        {
            if (!type.IsGenericType)
                return type.Name;

            string name = type.Name;
            int arity = name.IndexOf('`');
            if (arity >= 0)
                name = name.Substring(0, arity);

            Type[] arguments = type.GetGenericArguments();
            var names = new string[arguments.Length];

            for (int i = 0; i < arguments.Length; i++)
                names[i] = arguments[i].IsGenericParameter ? arguments[i].Name : NameOf(arguments[i]);

            return name + "<" + string.Join(", ", names) + ">";
        }
    }
}
