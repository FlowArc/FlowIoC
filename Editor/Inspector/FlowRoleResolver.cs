#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Text;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Root;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.BaseModule.ViewsMediators.View;
using FlowIoC.ScreenModule.ViewsMediators.Screen;

namespace FlowIoC.Editor.Inspector
{
    /// <summary>
    /// What role a component plays, in the order the answers can be trusted: what the type says
    /// about itself, then what it derives from, then what it is called. A type that answers none
    /// of the three is not FlowIoC's to decorate, and its inspector is left exactly as it was.
    ///
    /// Test is deliberately absent from the rules. A View in a test module is still a View, so
    /// that role is only ever given by hand.
    /// </summary>
    internal class FlowRoleResolver
    {
        private readonly Dictionary<Type, FlowRole?> _cache = new Dictionary<Type, FlowRole?>();

        public bool TryResolve(Type type, out FlowRole role)
        {
            if (!_cache.TryGetValue(type, out FlowRole? cached))
            {
                cached = Resolve(type);
                _cache[type] = cached;
            }

            role = cached ?? default;

            return cached.HasValue;
        }

        /// <summary>
        /// What the bar says. The attribute wins when it names a title; otherwise the type's own
        /// name, split at its capitals so PlayerRoot reads as two words.
        /// </summary>
        public string TitleFor(Type type)
        {
            var header = (FlowHeaderAttribute) Attribute.GetCustomAttribute(type, typeof(FlowHeaderAttribute));

            if (header != null && !string.IsNullOrEmpty(header.Title))
                return header.Title.ToUpperInvariant();

            return Spaced(type.Name).ToUpperInvariant();
        }

        /// <summary>
        /// What the strip under the bar calls the type. The role name, unless the type says
        /// otherwise - a type may wear a role's colour without claiming to be one.
        /// </summary>
        public string LabelFor(Type type, FlowRole role)
        {
            var header = (FlowHeaderAttribute) Attribute.GetCustomAttribute(type, typeof(FlowHeaderAttribute));

            if (header != null && !string.IsNullOrEmpty(header.Label))
                return header.Label.ToUpperInvariant();

            // A Root wearing another role's colour still says that it is a Root, or the scene
            // would have no way to tell the module's presence from the thing it roots.
            if (role != FlowRole.Root && typeof(IRoot).IsAssignableFrom(type))
                return $"{role.ToString().ToUpperInvariant()} · ROOT";

            return role.ToString().ToUpperInvariant();
        }

        private FlowRole? Resolve(Type type)
        {
            var header = (FlowHeaderAttribute) Attribute.GetCustomAttribute(type, typeof(FlowHeaderAttribute));

            if (header != null)
                return header.Role;

            if (typeof(IScreenBody).IsAssignableFrom(type))
                return FlowRole.Screen;

            if (typeof(IRoot).IsAssignableFrom(type))
                return RoleOfRoot(type);

            if (typeof(IView).IsAssignableFrom(type))
                return FlowRole.View;

            if (typeof(IMediator).IsAssignableFrom(type))
                return FlowRole.Mediator;

            return RoleOfName(type.Name);
        }

        /// <summary>
        /// A Root takes the colour of whatever it roots. A Service and a System are not components
        /// and a Connector is a sub-context, so their Roots are the only place those colours are
        /// ever seen - ScreenServiceRoot reads as a Service, ConnectorRoot as a Connector, and a
        /// game module's own Root stays a Root.
        /// </summary>
        private FlowRole RoleOfRoot(Type type)
        {
            string rooted = type.Name.EndsWith("Root")
                ? type.Name.Substring(0, type.Name.Length - "Root".Length)
                : type.Name;

            return RoleOfName(rooted) ?? FlowRole.Root;
        }

        private FlowRole? RoleOfName(string name)
        {
            if (name.EndsWith("Service"))
                return FlowRole.Service;

            if (name.EndsWith("System"))
                return FlowRole.System;

            if (name.EndsWith("Adapter"))
                return FlowRole.Adapter;

            if (name.Contains("Connector"))
                return FlowRole.Connector;

            return null;
        }

        private string Spaced(string name)
        {
            var spaced = new StringBuilder(name.Length + 4);

            for (int ii = 0; ii < name.Length; ii++)
            {
                if (ii > 0 && char.IsUpper(name[ii]) && !char.IsUpper(name[ii - 1]))
                    spaced.Append(' ');

                spaced.Append(name[ii]);
            }

            return spaced.ToString();
        }
    }
}

#endif