using System;
using System.Collections.Generic;

namespace FlowIoC.BaseModule.Root.Utils
{
    /// <summary>
    /// Finds a context type by the name a Root has written down for it. Every Root used to ask the
    /// domain for its types afresh and then scan the answer by name, so a scene of fifteen Roots
    /// walked every type in every loaded assembly fifteen times before the first binding was
    /// declared. The list is read once here and kept as a lookup.
    ///
    /// It is an instance rather than a static cache, and the run's <see cref="RootsManager"/> owns
    /// it: a lookup that lives as long as the run ends with it, instead of surviving into the next
    /// one and having to be reset.
    /// </summary>
    internal sealed class ContextTypeIndex
    {
        private Dictionary<string, Type> _byFullName;

        /// <summary>
        /// The context type declared under this full name, or null when the project holds no such
        /// context - which is what a Root listing a context that has since been renamed or deleted
        /// looks like, and is reported by the caller rather than thrown here.
        /// </summary>
        public Type Resolve(string contextFullName)
        {
            if (string.IsNullOrEmpty(contextFullName))
                return null;

            _byFullName ??= Build();

            return _byFullName.TryGetValue(contextFullName, out Type contextType) ? contextType : null;
        }

        private static Dictionary<string, Type> Build()
        {
            List<Type> contextTypes = AssemblyExtensions.GetAllContextTypes();
            Dictionary<string, Type> byFullName = new Dictionary<string, Type>(contextTypes.Count, StringComparer.Ordinal);

            for (int i = 0; i < contextTypes.Count; i++)
            {
                string fullName = contextTypes[i].FullName;
                if (fullName != null)
                    byFullName[fullName] = contextTypes[i];
            }

            return byFullName;
        }
    }
}
