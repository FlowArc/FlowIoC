#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;

namespace FlowIoC.Editor.ModuleScanner
{
    /// <summary>
    /// Every assembly the project compiles, by the asmdef that declares it: everything under
    /// Assets and Packages, and the package's own assemblies wherever the Package Manager resolved
    /// the package to.
    ///
    /// A package installed from a registry sits under Library/PackageCache, outside both folders.
    /// A list read off Assets and Packages alone had no FlowIoC or FlowIoC.Editor in it, so the
    /// orphan sweep reported the .csproj.DotSettings PackageNamespaceFoldersWriter writes for
    /// them - and deleted them, for the next startup to write back. A submodule install sits
    /// under Packages, which is why the project FlowIoC is developed in never showed it.
    ///
    /// A folder whose name ends in ~ or starts with a dot is skipped, because Unity compiles
    /// nothing in it. The package keeps its installable modules in Modules~ and SetupModules~,
    /// and counting their asmdefs would keep the settings of a module the project deleted alive
    /// for as long as the package ships a module of the same name.
    ///
    /// The scan and the rename planner both ask here: two answers to which assemblies exist is
    /// how the gap went unnoticed.
    /// </summary>
    internal class ProjectAssemblyNames
    {
        private const string ASSETS = "Assets";
        private const string PACKAGES = "Packages";
        private const string ASSEMBLY_DEFINITION_PATTERN = "*.asmdef";

        private readonly Func<string> _packageRoot;

        internal ProjectAssemblyNames() : this(() =>
            UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(ProjectAssemblyNames).Assembly)?.resolvedPath)
        {
        }

        internal ProjectAssemblyNames(Func<string> packageRoot)
        {
            _packageRoot = packageRoot;
        }

        internal List<string> OnDisk(string projectRoot)
        {
            var names = new List<string>();

            foreach (string searchPath in SearchPaths(projectRoot))
            {
                if (Directory.Exists(searchPath))
                    Collect(searchPath, names);
            }

            return names;
        }

        private IEnumerable<string> SearchPaths(string projectRoot)
        {
            string packages = Path.Combine(projectRoot, PACKAGES);

            yield return Path.Combine(projectRoot, ASSETS);
            yield return packages;

            string packageRoot = _packageRoot();

            // A submodule or an embedded install is already under Packages, and walking it twice
            // would list every one of its assemblies twice.
            if (!string.IsNullOrEmpty(packageRoot) && !IsUnder(packageRoot, packages))
                yield return packageRoot;
        }

        private void Collect(string folder, List<string> names)
        {
            foreach (string asmdef in Directory.GetFiles(folder, ASSEMBLY_DEFINITION_PATTERN, SearchOption.TopDirectoryOnly))
                names.Add(Path.GetFileNameWithoutExtension(asmdef));

            foreach (string child in Directory.GetDirectories(folder))
            {
                string name = Path.GetFileName(child);

                if (name.EndsWith("~", StringComparison.Ordinal) || name.StartsWith(".", StringComparison.Ordinal))
                    continue;

                Collect(child, names);
            }
        }

        private bool IsUnder(string path, string folder)
        {
            string full = Path.GetFullPath(path).TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
            string parent = Path.GetFullPath(folder).TrimEnd('\\', '/') + Path.DirectorySeparatorChar;

            return full.StartsWith(parent, StringComparison.OrdinalIgnoreCase);
        }
    }
}

#endif
