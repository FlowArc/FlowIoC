#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Root;
using FlowIoC.Editor.CodeGenerator.Menus.Module;
using FlowIoC.Editor.Inspector;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.Modules
{
    /// <summary>
    /// The word at the right edge of a module row, in the colour its Root wears in the inspector:
    /// SYSTEM, SERVICE, CONNECTOR or CORE for a module, read off its Root the way the Root's own
    /// header bar reads it, and SCREEN or TEST for the two kinds whose kind is their role. A
    /// module whose Root cannot be asked - the project does not compile, or the module has no
    /// Root - falls back to its kind in the row's own grey.
    /// </summary>
    internal class ModuleRoleBadge
    {
        private readonly FlowPalette _palette = new FlowPalette();
        private readonly FlowRoleResolver _roles = new FlowRoleResolver();
        private readonly ModuleAssemblyName _assemblies = new ModuleAssemblyName();

        /// <summary>
        /// One answer per module. The Root's type does not change without a domain reload, and a
        /// reload takes this with it.
        /// </summary>
        private readonly Dictionary<string, FlowRole?> _cache = new Dictionary<string, FlowRole?>();

        public string Text(IModuleTreeItem module)
        {
            FlowRole? role = RoleOf(module);

            return (role.HasValue ? role.Value.ToString() : module.Kind.ToString()).ToUpperInvariant();
        }

        public GUIStyle Style(IModuleTreeItem module, FlowRowPainter rows, bool hovered)
        {
            FlowRole? role = RoleOf(module);

            return role.HasValue
                ? rows.BadgeIn(_palette.Accent(role.Value, EditorGUIUtility.isProSkin))
                : rows.Badge(hovered);
        }

        private FlowRole? RoleOf(IModuleTreeItem module)
        {
            switch (module.Kind)
            {
                case ModuleKind.Screen: return FlowRole.Screen;
                case ModuleKind.Test: return FlowRole.Test;
            }

            if (_cache.TryGetValue(module.Name, out FlowRole? cached)) return cached;

            FlowRole? role = RoleOfRoot(module.Name);
            _cache[module.Name] = role;

            return role;
        }

        /// <summary>
        /// What the module's Root says it roots, asked of FlowRoleResolver so that a Root
        /// declaring itself with [FlowHeader(FlowRole.Core)] answers the same here as in its own
        /// inspector. The plain Root is no answer: it is what the resolver says when the Root has
        /// not said what it roots, and the badge then has nothing to say either.
        /// </summary>
        private FlowRole? RoleOfRoot(string moduleName)
        {
            string assemblyName = _assemblies.From(moduleName);

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name != assemblyName) continue;

                foreach (Type type in SafeTypes(assembly))
                {
                    if (type.IsAbstract || !typeof(IRoot).IsAssignableFrom(type)) continue;
                    if (!_roles.TryResolve(type, out FlowRole role) || role == FlowRole.Root) continue;

                    return role;
                }
            }

            return null;
        }

        /// <summary>A type that cannot be loaded has no role, and is not a reason to draw nothing.</summary>
        private IEnumerable<Type> SafeTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                var loaded = new List<Type>();

                foreach (Type type in exception.Types)
                {
                    if (type != null) loaded.Add(type);
                }

                return loaded;
            }
        }
    }
}

#endif
