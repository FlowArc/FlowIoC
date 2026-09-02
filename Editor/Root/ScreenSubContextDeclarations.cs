#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using FlowIoC.BaseModule.Root.Utils;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.RootsContexts;

namespace FlowIoC.Editor.Root
{
    /// <summary>
    /// Reads what a screen context declares in code so the Root's inspector can show it. A screen
    /// context is a plain class with no constructor, so it is instantiated to read the property -
    /// which is why a context whose declaration reaches for an injected member is caught here
    /// rather than allowed to break the inspector.
    ///
    /// One instance per inspector, rebuilt on enable: a repaint then costs nothing and a recompile
    /// does not serve stale values.
    /// </summary>
    internal class ScreenSubContextDeclarations
    {
        private readonly Dictionary<string, Type> _types = new();
        private readonly Dictionary<Type, ScreenCVO> _declarations = new();
        private readonly Dictionary<Type, string> _failures = new();

        internal Type ResolveType(string contextFullName)
        {
            if (string.IsNullOrEmpty(contextFullName))
                return null;

            if (_types.TryGetValue(contextFullName, out Type cached))
                return cached;

            Type resolved = AssemblyExtensions.GetAllContextTypes()
                .FirstOrDefault(type => type.FullName == contextFullName);

            _types[contextFullName] = resolved;
            return resolved;
        }

        internal bool IsScreenContext(Type contextType)
        {
            return contextType != null
                   && !contextType.IsAbstract
                   && typeof(ScreenSubContextBase).IsAssignableFrom(contextType);
        }

        internal bool TryRead(Type contextType, out ScreenCVO declaration, out string error)
        {
            declaration = null;
            error = null;

            if (!IsScreenContext(contextType))
                return false;

            if (_declarations.TryGetValue(contextType, out declaration))
                return true;

            if (_failures.TryGetValue(contextType, out error))
                return false;

            try
            {
                ScreenSubContextBase context = (ScreenSubContextBase) Activator.CreateInstance(contextType);
                declaration = context.Declaration;
                _declarations[contextType] = declaration;
                return true;
            }
            catch (Exception exception)
            {
                declaration = null;
                error = $"{contextType.Name} could not be read: {(exception.InnerException ?? exception).Message}";
                _failures[contextType] = error;
                return false;
            }
        }
    }
}
#endif
