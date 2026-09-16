#if UNITY_EDITOR

using System.Reflection;
using UnityEditor.PackageManager;

namespace FlowIoC.Editor.ModuleInstall
{
    /// <summary>
    /// Where a module page's files are. A module ships in the package that carries the page that
    /// installs it, under that package's Modules~, so the package is read from the page's
    /// assembly rather than named here. FlowIoC therefore contains no reference to any other
    /// package, and a second one works without a line changing - FlowIoC's own pages resolve to
    /// FlowIoC the same way.
    ///
    /// The path is asked of the Package Manager for the same reason ModulesSource asks: a UPM
    /// install resolves to a hashed folder under Library/PackageCache, and a submodule resolves
    /// to Packages/&lt;name&gt;.
    /// </summary>
    internal class ModulePayload
    {
        internal ModulePayload(Assembly assembly)
            : this(PackageInfo.FindForAssembly(assembly))
        {
        }

        private ModulePayload(PackageInfo info)
            : this(info?.resolvedPath, info?.name, info?.version)
        {
        }

        internal ModulePayload(string packageRoot) : this(packageRoot, string.Empty, string.Empty)
        {
        }

        internal ModulePayload(string packageRoot, string packageName, string packageVersion)
        {
            PackageRoot = packageRoot;
            PackageName = packageName ?? string.Empty;
            PackageVersion = packageVersion ?? string.Empty;
        }

        /// <summary>The package the page came from, or null when it came from none.</summary>
        internal string PackageRoot { get; }

        /// <summary>The package's id and version, empty when the page came from no package.</summary>
        internal string PackageName { get; }

        internal string PackageVersion { get; }

        internal bool IsResolved => !string.IsNullOrEmpty(PackageRoot);

        /// <summary>
        /// The modules this package ships, or null when there is no package to read them from.
        /// Null is what lets the page draw itself as unavailable rather than offering an install
        /// that could only fail.
        /// </summary>
        internal ModulesSource Source() =>
            IsResolved ? new ModulesSource(PackageRoot) : null;
    }
}

#endif
