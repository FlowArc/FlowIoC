#if UNITY_EDITOR

using System.Reflection;
using UnityEditor.PackageManager;

namespace FlowIoC.Editor.ModuleInstall
{
    /// <summary>
    /// Where a private module's files are. A private module ships in a package of its own
    /// alongside the page that installs it, so the package is read from the page's assembly
    /// rather than named here. FlowIoC therefore contains no reference to any private package,
    /// and a second one would work without a line changing.
    ///
    /// The path is asked of the Package Manager for the same reason ModulesSource asks: a UPM
    /// install resolves to a hashed folder under Library/PackageCache, and a submodule resolves
    /// to Packages/&lt;name&gt;.
    /// </summary>
    internal class PrivateModulePayload
    {
        /// <summary>
        /// The folder ends in a tilde so Unity does not import it. The modules inside carry
        /// asmdefs of their own and would otherwise compile inside a package nobody can edit -
        /// the same reason Modules~ is spelled that way.
        /// </summary>
        internal const string Folder = "PrivateModules~";

        internal PrivateModulePayload(Assembly assembly)
            : this(PackageInfo.FindForAssembly(assembly)?.resolvedPath)
        {
        }

        internal PrivateModulePayload(string packageRoot)
        {
            PackageRoot = packageRoot;
        }

        /// <summary>The package the page came from, or null when it came from none.</summary>
        internal string PackageRoot { get; }

        internal bool IsResolved => !string.IsNullOrEmpty(PackageRoot);

        /// <summary>
        /// The modules this package ships, or null when there is no package to read them from.
        /// Null is what lets the page draw itself as unavailable rather than offering an install
        /// that could only fail.
        /// </summary>
        internal ModulesSource Source() =>
            IsResolved ? new ModulesSource(PackageRoot, Folder) : null;
    }
}

#endif
