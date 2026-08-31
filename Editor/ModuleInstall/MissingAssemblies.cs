#if UNITY_EDITOR

using System;
using System.Collections.Generic;

namespace FlowIoC.Editor.ModuleInstall
{
    /// <summary>
    /// Which of a module's assembly requirements are not in this project. A private module is
    /// private because it is built on a paid asset, and no Package Manager entry brings one: the
    /// asset is imported into Assets or it is not there. Copying the module in without it would
    /// leave an asmdef referencing an assembly that does not exist, which stops the whole project
    /// compiling.
    ///
    /// Nothing Unity is touched here. The caller hands in what it found and what the module asks
    /// for, which is what makes the answer worth testing on its own - the same division
    /// MissingPackages and InstalledPackages already make.
    /// </summary>
    internal class MissingAssemblies
    {
        /// <summary>
        /// The requirements absent from <paramref name="loadedAssemblyNames"/>, in the order they
        /// were asked for and without repeats. A null list is not an empty project: it is a
        /// project whose assemblies are not known, so nothing can be ruled out.
        /// </summary>
        public IReadOnlyList<string> In(
            IEnumerable<string> loadedAssemblyNames,
            IEnumerable<string> requiredAssemblyNames)
        {
            var loaded = new HashSet<string>(StringComparer.Ordinal);

            if (loadedAssemblyNames != null)
            {
                foreach (string name in loadedAssemblyNames)
                    loaded.Add(name);
            }

            var missing = new List<string>();
            var reported = new HashSet<string>(StringComparer.Ordinal);

            if (requiredAssemblyNames == null)
                return missing;

            foreach (string name in requiredAssemblyNames)
            {
                if (loaded.Contains(name) || !reported.Add(name))
                    continue;

                missing.Add(name);
            }

            return missing;
        }
    }
}

#endif
