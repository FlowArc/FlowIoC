#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlowIoC.BaseModule.ProjectPaths;
using FlowIoC.Editor.CodeGenerator;
using FlowIoC.Editor.CodeGenerator.Menus.Module;
using FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration;
using FlowIoC.Editor.Config.ModuleConfig;
using FlowIoC.Editor.Modules;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.ModuleScanner
{
    /// <summary>
    /// Turns the project into the targets the checks are pointed at.
    ///
    /// This is the one class in the scan that talks to Unity - AssetDatabase, Application, the
    /// settings assets - which is what keeps every check testable without an editor running.
    /// </summary>
    internal class ModuleTargetFactory
    {
        private readonly Func<ModuleKind, DirectoryStructureConfig> _layoutFor;

        internal ModuleTargetFactory() : this(new DirectoryStructureConfigProvider().ConfigFor)
        {
        }

        internal ModuleTargetFactory(Func<ModuleKind, DirectoryStructureConfig> layoutFor)
        {
            _layoutFor = layoutFor;
        }

        internal (ProjectTargetEVO Project, List<ModuleTargetEVO> Modules) Build()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

            var settings = AssetDatabase.LoadAssetAtPath<ED_CodeGenerator>(
                new FlowIoCProjectPaths().CodeGeneratorSettings);

            // Without the settings there is no way to tell a sub module from a screen module, and
            // a scan that guessed would report every nested module as the wrong kind.
            if (settings == null)
                return (new ProjectTargetEVO {ProjectRoot = projectRoot}, new List<ModuleTargetEVO>());

            List<ScannedModule> scanned = Scan(settings, projectRoot);

            return (ProjectFrom(projectRoot, scanned), ModulesFrom(projectRoot, scanned));
        }

        private List<ScannedModule> Scan(ED_CodeGenerator settings, string projectRoot)
        {
            var resolver = new ModuleKindResolver(
                settings.FolderNameFor(FolderEVO.FolderType.SubModules, "zSubModules"),
                settings.FolderNameFor(FolderEVO.FolderType.ScreenModules, "zScreenModules"),
                settings.FolderNameFor(FolderEVO.FolderType.TestModules, "zTestModules"));

            var scanner = new ModuleTreeScanner(resolver);
            var scanned = new List<ScannedModule>();

            foreach (string root in new ModuleScannerRoots().All(projectRoot))
                scanned.AddRange(scanner.Scan(root));

            return scanned;
        }

        private ProjectTargetEVO ProjectFrom(string projectRoot, List<ScannedModule> scanned)
        {
            return new ProjectTargetEVO
            {
                ProjectRoot = projectRoot,
                ScannedModules = scanned,
                Index = new ModuleIndexProvider().LoadOrCreate(),
                AllAssemblyNames = AssemblyNames(projectRoot)
            };
        }

        private List<ModuleTargetEVO> ModulesFrom(string projectRoot, List<ScannedModule> scanned)
        {
            var names = new ModuleAssemblyName();
            var shared = new SharedAssemblyDefinition();
            var signals = new SignalsAssemblyDefinition();
            var paths = new ModuleAssetPathResolver();

            // A layout is asked for once per kind and shared by every module of that kind. The
            // lookup behind it runs the path migrator's probe - a walk of every module root - on
            // every call, and asked per module, and twice more per nested module for its parent,
            // that probe was two thirds of a scan that runs whenever the window is focused.
            var layouts = new Dictionary<ModuleKind, DirectoryStructureConfig>();

            DirectoryStructureConfig LayoutFor(ModuleKind kind)
            {
                if (!layouts.TryGetValue(kind, out DirectoryStructureConfig layout))
                    layouts[kind] = layout = _layoutFor(kind);

                return layout;
            }

            var modules = new List<ModuleTargetEVO>();

            foreach (ScannedModule module in scanned.OrderBy(found => found.Name, StringComparer.Ordinal))
            {
                string parent = ParentModulePathOf(module.AbsolutePath, scanned);

                modules.Add(new ModuleTargetEVO
                {
                    Name = module.Name,
                    Kind = module.Kind,
                    AbsolutePath = module.AbsolutePath,
                    AssetPath = paths.ToAssetPath(module.AbsolutePath),
                    Layout = LayoutFor(module.Kind),
                    ParentAbsolutePath = parent,
                    ParentName = parent == null ? null : Path.GetFileName(parent),
                    ParentSharedAssemblyName = parent == null
                        ? null
                        : shared.FindIn(parent, LayoutFor(ModuleKind.Main)),
                    ParentSignalsAssemblyName = parent == null
                        ? null
                        : signals.FindIn(parent, LayoutFor(ModuleKind.Main)),
                    ParentAssemblyName = parent == null ? null : names.From(Path.GetFileName(parent)),
                    ExpectedAssemblyName = names.From(module.Name),
                    ProjectRoot = projectRoot
                });
            }

            return modules;
        }

        /// <summary>
        /// The nearest scanned module above this one, or null for a top level module. Read off
        /// the scan rather than off the folder names, so a Sub, Screen or Test module all find
        /// the same parent through whichever container folder they sit in.
        /// </summary>
        private string ParentModulePathOf(string modulePath, List<ScannedModule> scanned)
        {
            string normalized = modulePath.Replace('\\', '/');
            string best = null;

            foreach (ScannedModule candidate in scanned)
            {
                string candidatePath = candidate.AbsolutePath.Replace('\\', '/');

                if (candidatePath == normalized) continue;
                if (!normalized.StartsWith(candidatePath + "/", StringComparison.OrdinalIgnoreCase)) continue;

                if (best == null || candidatePath.Length > best.Length)
                    best = candidate.AbsolutePath;
            }

            return best;
        }

        /// <summary>
        /// Every assembly the project defines, which is what tells an orphaned settings file from
        /// one that is still in use.
        /// </summary>
        private IReadOnlyList<string> AssemblyNames(string projectRoot)
        {
            var names = new List<string>();

            foreach (string searchPath in new[]
                     {
                         Path.Combine(projectRoot, "Assets"),
                         Path.Combine(projectRoot, "Packages")
                     })
            {
                if (!Directory.Exists(searchPath)) continue;

                foreach (string asmdef in Directory.GetFiles(searchPath, "*.asmdef", SearchOption.AllDirectories))
                    names.Add(Path.GetFileNameWithoutExtension(asmdef));
            }

            return names;
        }
    }
}

#endif