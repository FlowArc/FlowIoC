#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Reflection;

namespace FlowIoC.Editor.ModuleInstall
{
    /// <summary>
    /// The assemblies this Editor has loaded, by name. A page asks this while it draws, so it
    /// reads what is already in memory rather than requesting anything.
    ///
    /// It exists so that MissingAssemblies stays free of the runtime and can be tested on its
    /// own, the way InstalledPackages does for MissingPackages.
    /// </summary>
    internal class LoadedAssemblies
    {
        internal IReadOnlyList<string> Names()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var names = new List<string>(assemblies.Length);

            foreach (Assembly assembly in assemblies)
                names.Add(assembly.GetName().Name);

            return names;
        }
    }
}

#endif
