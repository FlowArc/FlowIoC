#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlowIoC.BaseModule.ProjectPaths;
using FlowIoC.Editor.CodeGenerator;
using FlowIoC.Editor.Config.ModuleConfig;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.Modules
{
    internal class LogTypeChanges
    {
        public List<string> ToAdd = new List<string>();
        public List<string> ToRemove = new List<string>();
    }

    /// <summary>
    /// What has to change for the auto-registered log types to match the modules that exist.
    /// Only auto-registered names are passed in, so a channel the project added by hand is
    /// never proposed for removal.
    /// </summary>
    internal class ModuleLogTypePlan
    {
        public LogTypeChanges Plan(IEnumerable<string> registeredAutoTypes, IEnumerable<string> moduleNames)
        {
            var registered = new HashSet<string>(
                registeredAutoTypes ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            var modules = new HashSet<string>(
                moduleNames ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);

            // A scan that found no modules at all is far more likely to be a failed scan than a
            // project that genuinely has none. Proposing removals from an empty module list would
            // wipe every auto-registered channel on the strength of that failure, so removals are
            // skipped entirely unless at least one module was actually found.
            List<string> toRemove = modules.Count == 0
                ? new List<string>()
                : registered.Where(r => !modules.Contains(r)).ToList();

            return new LogTypeChanges
            {
                ToAdd = modules.Where(m => !registered.Contains(m)).ToList(),
                ToRemove = toRemove
            };
        }
    }

    internal class ModuleIndexRebuilder
    {
        private readonly FlowIoCProjectPaths _paths = new FlowIoCProjectPaths();

        /// <summary>
        /// The rebuilt index, or null when the code generator settings could not be loaded and
        /// the index was left as it is. Returning it rather than leaving callers to load the
        /// index themselves is what keeps a failed rebuild distinguishable from a successful
        /// one: a caller that loaded it independently would get an empty index either way, and
        /// go on to write into it as though the rebuild had happened.
        /// </summary>
        public FlowIoCModuleIndex Rebuild()
        {
            var settings = AssetDatabase.LoadAssetAtPath<CodeGeneratorSettings>(_paths.CodeGeneratorSettings);
            if (settings == null)
            {
                Debug.LogWarning("<color=cyan>FlowIoC:</color> the code generator settings could not be " +
                                 "loaded, so the module index was left as it is.");
                return null;
            }

            var resolver = new ModuleKindResolver(
                settings.FolderNameFor(FolderConfig.FolderType.SubModules, "zSubModules"),
                settings.FolderNameFor(FolderConfig.FolderType.ScreenModules, "zScreenModules"),
                settings.FolderNameFor(FolderConfig.FolderType.TestModules, "zTestModules"));

            var scanner = new ModuleTreeScanner(resolver);
            var scanned = new List<ScannedModule>();

            scanned.AddRange(scanner.Scan(Path.Combine(Application.dataPath, "Modules")));
            foreach (string modulesRoot in EmbeddedPackageModuleRoots())
                scanned.AddRange(scanner.Scan(modulesRoot));

            FlowIoCModuleIndex index = new ModuleIndexProvider().LoadOrCreate();

            index.Replace(new ModuleIndexBuilder().Build(scanned, GuidOfAbsolutePath, index.Modules));

            EditorUtility.SetDirty(index);
            AssetDatabase.SaveAssets();

            return index;
        }

        private IEnumerable<string> EmbeddedPackageModuleRoots()
        {
            string packagesPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Packages"));
            if (!Directory.Exists(packagesPath)) yield break;

            foreach (string packageDir in Directory.GetDirectories(packagesPath))
            foreach (string modulesDir in Directory.GetDirectories(packageDir, "Modules", SearchOption.AllDirectories))
                yield return modulesDir;
        }

        private string GuidOfAbsolutePath(string absolutePath)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (projectRoot == null) return string.Empty;

            string relative = Path.GetRelativePath(projectRoot, absolutePath).Replace('\\', '/');
            return AssetDatabase.AssetPathToGUID(relative);
        }
    }
}

#endif