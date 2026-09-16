#if UNITY_EDITOR

using System;
using System.Collections.Generic;

namespace FlowIoC.Editor.Help
{
    /// <summary>
    /// Which modules are new. A first record flags nothing - on a first open every module is
    /// uninstalled, and flagging all of them says nothing. A version change flags the modules
    /// the previous version did not ship, and the flags stay until the version changes again.
    /// </summary>
    internal class ModuleLibraryArrivalsRule
    {
        internal ModuleLibraryArrivalsEVO Next(ModuleLibraryArrivalsEVO previous, string version,
            IReadOnlyList<string> shipped)
        {
            var current = new List<string>(shipped);
            current.Sort(StringComparer.Ordinal);

            if (previous == null)
                return new ModuleLibraryArrivalsEVO {Version = version, Shipped = current.ToArray()};

            if (previous.Version == version)
                return previous;

            var arrived = new List<string>();

            foreach (string folder in current)
            {
                if (Array.IndexOf(previous.Shipped, folder) < 0)
                    arrived.Add(folder);
            }

            return new ModuleLibraryArrivalsEVO
            {
                Version = version, Shipped = current.ToArray(), New = arrived.ToArray()
            };
        }
    }
}

#endif
