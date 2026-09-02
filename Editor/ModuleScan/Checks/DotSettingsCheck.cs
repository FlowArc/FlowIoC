#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration;

namespace FlowIoC.Editor.ModuleScan
{
    /// <summary>
    /// The .csproj.DotSettings files at the project root that tell Rider which of a module's
    /// folders produce a namespace.
    ///
    /// They live beside the .csproj, which is at the project root. A copy inside the module
    /// folder has no effect whatsoever, which is exactly what the retired Assembly Creator window
    /// and the Update Module's Namespaces context menu were both writing.
    ///
    /// A module owns one file per assembly it has - its own and its Shared assembly's - because
    /// such a file only applies to the project it is named after, so the module's own file cannot
    /// skip the Scripts folder on Shared's behalf.
    /// </summary>
    internal class DotSettingsCheck : IModuleCheck
    {
        private const string EXTENSION = ".csproj.DotSettings";

        private readonly DotSettingsPlan _plan;
        private readonly DotSettingsFile _file;
        private readonly Func<ModuleTargetEVO, IReadOnlyList<string>> _settingsPathsOf;
        private readonly Func<ModuleTargetEVO, string> _modulesRootOf;

        internal DotSettingsCheck() : this(
            new DotSettingsPlan(),
            new DotSettingsFile(),
            DefaultSettingsPaths,
            DefaultModulesRootOf)
        {
        }

        internal DotSettingsCheck(
            DotSettingsPlan plan,
            DotSettingsFile file,
            Func<ModuleTargetEVO, IReadOnlyList<string>> settingsPathsOf,
            Func<ModuleTargetEVO, string> modulesRootOf)
        {
            _plan = plan;
            _file = file;
            _settingsPathsOf = settingsPathsOf;
            _modulesRootOf = modulesRootOf;
        }

        public string Id => "dotsettings";

        public FindingEVO Inspect(ModuleTargetEVO module)
        {
            IReadOnlyList<string> paths = _settingsPathsOf(module);

            // No assembly means no project, so no settings file is owed. That gap is
            // AssemblyDefinitionCheck's finding to report.
            if (paths.Count == 0)
                return FindingEVO.Ok(Id, "Namespace settings (no assembly yet)");

            IReadOnlyList<string> skip = _plan.SkipFoldersFor(module, _modulesRootOf(module));
            var stale = new List<string>();

            foreach (string path in paths)
            {
                if (!_file.Matches(path, skip)) stale.Add(Path.GetFileName(path));
            }

            if (stale.Count == 0)
                return FindingEVO.Ok(Id, "Namespace settings");

            return FindingEVO.Fixable(Id, $"Namespace settings missing or stale: {string.Join(", ", stale)}");
        }

        public void Fix(ModuleTargetEVO module)
        {
            IReadOnlyList<string> skip = _plan.SkipFoldersFor(module, _modulesRootOf(module));

            foreach (string path in _settingsPathsOf(module))
                _file.Write(path, skip);
        }

        /// <summary>
        /// The module's own assembly and its Shared assembly, read off the files on disk rather
        /// than derived from the module name - a module may have been renamed since it was
        /// created, and the settings file has to follow the assembly, not the folder.
        /// </summary>
        private static IReadOnlyList<string> DefaultSettingsPaths(ModuleTargetEVO module)
        {
            var paths = new List<string>();

            if (module == null || string.IsNullOrEmpty(module.AbsolutePath) || !Directory.Exists(module.AbsolutePath))
                return paths;

            string[] asmdefs = Directory.GetFiles(module.AbsolutePath, "*.asmdef", SearchOption.TopDirectoryOnly);
            if (asmdefs.Length != 1) return paths;

            paths.Add(Path.Combine(module.ProjectRoot, Path.GetFileNameWithoutExtension(asmdefs[0]) + EXTENSION));

            string shared = new SharedAssemblyDefinition().FindIn(module.AbsolutePath, module.Layout);
            if (!string.IsNullOrEmpty(shared))
                paths.Add(Path.Combine(module.ProjectRoot, shared + EXTENSION));

            return paths;
        }

        /// <summary>
        /// The modules root the target sits under, so that a module shipped inside an embedded
        /// package is measured against its own package rather than against Assets/Modules.
        /// </summary>
        private static string DefaultModulesRootOf(ModuleTargetEVO module)
        {
            if (module == null || string.IsNullOrEmpty(module.AbsolutePath)) return null;

            foreach (string root in new ModuleScanRoots().All(module.ProjectRoot))
            {
                string normalized = root.Replace('\\', '/');

                if (module.AbsolutePath.Replace('\\', '/')
                    .StartsWith(normalized + "/", StringComparison.OrdinalIgnoreCase))
                    return root;
            }

            return Path.Combine(module.ProjectRoot ?? string.Empty, "Assets", "Modules");
        }
    }
}

#endif
