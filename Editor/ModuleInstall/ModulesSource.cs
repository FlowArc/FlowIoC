#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.AgentRules;
using UnityEditor.PackageManager;

namespace FlowIoC.Editor.ModuleInstall
{
    /// <summary>
    /// Finds the ready made modules FlowIoC ships. The package resolves to a hashed path under
    /// Library/PackageCache for a UPM install and to Packages/FlowIoC for a submodule, so the path
    /// is asked of the Package Manager rather than assumed.
    ///
    /// The payload lives in a folder ending in a tilde, which Unity does not import: the modules
    /// carry asmdefs of their own and would otherwise compile inside the package, where nobody
    /// could edit them.
    /// </summary>
    internal class ModulesSource
    {
        internal const string ModulesFolder = "Modules~";

        /// <summary>
        /// The setup modules sit in a folder of their own rather than under Modules~ with a marker
        /// on them. TryList is what fills the Help window's list of modules with an Install button,
        /// and a set member listed there would draw a button nobody should press. Two folders and
        /// two lists cost less than a rule that filters one list into the other.
        /// </summary>
        internal const string SetupModulesFolder = "SetupModules~";

        private readonly string _packageRootPath;
        private readonly string _modulesFolder;

        internal ModulesSource() : this(ResolvePackageRoot(), ModulesFolder)
        {
        }

        internal ModulesSource(string packageRootPath) : this(packageRootPath, ModulesFolder)
        {
        }

        internal ModulesSource(string packageRootPath, string modulesFolder)
        {
            _packageRootPath = packageRootPath;
            _modulesFolder = modulesFolder;
        }

        private static string ResolvePackageRoot()
        {
            var info = PackageInfo.FindForAssembly(typeof(ModulesSource).Assembly);

            return info != null
                ? info.resolvedPath
                : Path.Combine(new ProjectRoot().Resolve(), "Packages", "FlowIoC");
        }

        internal string Root => Path.Combine(_packageRootPath, _modulesFolder);

        internal string PathOf(string moduleFolderName) => Path.Combine(Root, moduleFolderName);

        /// <summary>
        /// The folders under Modules~ that are actually modules. A folder with no asmdef at its
        /// top is not one, and is stepped over rather than reported, so supporting material can
        /// live beside them.
        /// </summary>
        internal bool TryList(out string[] moduleFolders, out string error)
        {
            moduleFolders = Array.Empty<string>();
            error = null;

            if (!Directory.Exists(Root))
            {
                error = $"FlowIoC could not find its modules at '{Root}'. "
                        + $"Expected {_modulesFolder}/ inside the package.";
                return false;
            }

            try
            {
                var found = new List<string>();

                foreach (string folder in Directory.GetDirectories(Root))
                {
                    if (Directory.GetFiles(folder, "*.asmdef", SearchOption.TopDirectoryOnly).Length > 0)
                        found.Add(folder);
                }

                found.Sort(StringComparer.Ordinal);
                moduleFolders = found.ToArray();
                return true;
            }
            catch (Exception exception)
            {
                error = $"FlowIoC could not read '{Root}': {exception.Message}";
                return false;
            }
        }
    }
}

#endif