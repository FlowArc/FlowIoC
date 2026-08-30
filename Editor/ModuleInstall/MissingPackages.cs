#if UNITY_EDITOR

using System;
using System.Collections.Generic;

namespace FlowIoC.Editor.ModuleInstall
{
    /// <summary>
    /// Which of a module's package requirements this project does not have. A module that leans on
    /// Cinemachine cannot be copied into a project without it - the asmdef would reference an
    /// assembly that is not there and nothing would compile - so the install asks first.
    ///
    /// The Package Manager is not touched here. The caller hands in what it found and what the
    /// module asks for, which is what makes the answer worth testing on its own.
    /// </summary>
    internal class MissingPackages
    {
        /// <summary>
        /// The requirements absent from <paramref name="installedPackageIds"/>, in the order they
        /// were asked for and without repeats. A null installed list is not an empty project: it
        /// is a project whose packages are not known yet, so nothing can be ruled out.
        /// </summary>
        public IReadOnlyList<string> In(
            IEnumerable<string> installedPackageIds,
            IEnumerable<string> requiredPackageIds)
        {
            var installed = new HashSet<string>(StringComparer.Ordinal);

            if (installedPackageIds != null)
            {
                foreach (string id in installedPackageIds)
                    installed.Add(id);
            }

            var missing = new List<string>();
            var reported = new HashSet<string>(StringComparer.Ordinal);

            if (requiredPackageIds == null)
                return missing;

            foreach (string id in requiredPackageIds)
            {
                if (installed.Contains(id) || !reported.Add(id))
                    continue;

                missing.Add(id);
            }

            return missing;
        }
    }
}

#endif
