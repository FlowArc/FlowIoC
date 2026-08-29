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

        private readonly string _packageRootPath;

        internal ModulesSource()
        {
            var info = PackageInfo.FindForAssembly(typeof(ModulesSource).Assembly);

            _packageRootPath = info != null
                ? info.resolvedPath
                : Path.Combine(new ProjectRoot().Resolve(), "Packages", "FlowIoC");
        }

        internal ModulesSource(string packageRootPath)
        {
            _packageRootPath = packageRootPath;
        }

        internal string Root => Path.Combine(_packageRootPath, ModulesFolder);

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
                        + $"Expected {ModulesFolder}/ inside the package.";
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
