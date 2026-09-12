#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlowIoC.ConsoleModule;
using FlowIoC.Editor.Addressables;
using FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration;
using FlowIoC.Editor.Console;
using FlowIoC.Editor.ModuleCards;
using FlowIoC.Editor.Modules;
using FlowIoC.Editor.ModuleScanner;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.RenameModule
{
    /// <summary>What one rename did, line by line, and where the module is now.</summary>
    internal class ModuleRenameReportEVO
    {
        internal List<string> Lines { get; } = new List<string>();

        /// <summary>Scenes that were open, were changed, and were left for their owner to save.</summary>
        internal List<string> UnsavedScenes { get; } = new List<string>();

        /// <summary>Null when everything ran; otherwise what stopped it, with the lines above saying how far it got.</summary>
        internal string Failure { get; set; }

        /// <summary>The picked module's folder, as an asset path, where it is when the report is read.</summary>
        internal string ModuleAssetPath { get; set; }

        internal int SourceFilesRewritten { get; set; }
        internal int AssemblyDefinitionsRewritten { get; set; }
    }

    /// <summary>What the reload continuation has to know, kept in EditorPrefs across the reload.</summary>
    [Serializable]
    internal class PendingRenameEVO
    {
        public List<string> Modules = new List<string>();
        public string Parent;
    }

    /// <summary>
    /// Runs a rename plan, in the one order that keeps every step reading paths that still exist.
    ///
    /// The scene and prefab pass goes first, before any path changes, because the dependency index
    /// and the entry matching both read the module where it is. Then, with reloads locked so that
    /// nothing recompiles half-way: the asmdefs and every reference to them, the source text, the
    /// class files, the assets and the screen's address, the card headings, the folders - deepest
    /// first, so a follower is renamed at the path the plan knows before its parent moves - and the
    /// channel. Last, on a settled database, the things the Module Scanner already knows how to
    /// write: the index, the log type parts, the settings files, the orphan sweep. One refresh, one
    /// reload; after it, the continuation rewrites the cards.
    ///
    /// Nothing here opens a dialog, and the report carries every line, so this can be driven from
    /// a script or an agent as well as from the window - the same reason ModuleDeleter is shaped
    /// this way.
    /// </summary>
    internal class ModuleRenamer
    {
        internal const string PENDING_KEY = "FlowIoC.RenameModule.Pending";

        private const string SOURCE_PATTERN = "*.cs";
        private const string ASMDEF_PATTERN = "*.asmdef";
        private const string ASMDEF = ".asmdef";
        private const string TAG = "<color=cyan>[ModuleRenamer]</color> ";

        private readonly SourceTextRewriter _rewriter = new SourceTextRewriter();
        private readonly RenameRules _rules = new RenameRules();
        private readonly AssemblyDefinitionReferences _references = new AssemblyDefinitionReferences();
        private readonly AssemblyDefinitionDeclaredName _declared = new AssemblyDefinitionDeclaredName();
        private readonly ScreenAddressableEntries _addresses = new ScreenAddressableEntries();
        private readonly ModuleAssetPathResolver _paths = new ModuleAssetPathResolver();
        private readonly TextFile _files = new TextFile();
        private readonly ModuleCardHeading _headings = new ModuleCardHeading();

        /// <summary>
        /// The whole thing from two strings, every carrier ticked - what a script or the Editor walk
        /// calls. The window builds its own plan because it shows it first.
        /// </summary>
        internal ModuleRenameReportEVO Rename(string moduleName, string typedStem)
        {
            List<ModuleTreeRowEVO<ModulePickEVO>> tree =
                new ModuleTree().Build(new ModulePickFactory().From(new ModuleRegistryFactory().FromProject()));

            ModuleTreeRowEVO<ModulePickEVO> picked = tree.FirstOrDefault(row => row.Row.Name == moduleName);

            if (picked == null)
                return new ModuleRenameReportEVO {Failure = "No module named " + moduleName + " in the index."};

            return Run(new ModuleRenamePlan().Build(picked, typedStem, _ => true));
        }

        internal ModuleRenameReportEVO Run(ModuleRenamePlanEVO plan)
        {
            var report = new ModuleRenameReportEVO();

            if (!plan.CanRun)
            {
                report.Failure = string.Join(" ", plan.Blockers);

                return report;
            }

            ModuleRenameEVO picked = plan.Picked;
            string oldAssetPath = _paths.ToAssetPath(picked.OldPath);
            report.ModuleAssetPath = oldAssetPath;

            List<TextRule> global = _rules.Global(plan);
            List<TextRule> local = _rules.Local(plan);

            Debug.Log(TAG + "Renaming '" + picked.OldName + "' to '" + picked.NewName + "'...");

            foreach (SubContextRenameEVO outcome in new SubContextRenamer().Rename(oldAssetPath, global))
            {
                Log(outcome.Line(), report);

                if (outcome.SceneLeftUnsaved && !report.UnsavedScenes.Contains(outcome.AssetPath))
                    report.UnsavedScenes.Add(outcome.AssetPath);
            }

            EditorApplication.LockReloadAssemblies();

            try
            {
                RenameAssemblies(plan, report);
                RewriteSources(picked.OldPath, global, local, report);
                RenameClassFiles(plan, report);
                RenameAssets(plan, report);
                RenameCardHeadings(plan, report);
                RenameFolders(plan, report);
                report.ModuleAssetPath = _paths.ToAssetPath(NewPath(picked));
                RenameChannels(plan, report);
                SettleProject(picked, report);
                Remember(plan);
            }
            catch (Exception exception)
            {
                report.Failure = exception.Message;
                Debug.LogException(exception);
                Log("Stopped: " + exception.Message + ". Run Tools/FlowIoC/Module Scanner and repair what it reports.", report);
            }
            finally
            {
                AssetDatabase.SaveAssets();
                EditorApplication.UnlockReloadAssemblies();
                AssetDatabase.Refresh();
            }

            Debug.Log(TAG + "Renamed '" + picked.OldName + "' to '" + picked.NewName + "':\n" + string.Join("\n", report.Lines));

            return report;
        }

        /// <summary>
        /// The card blocks name the assemblies and the sub modules, and describing a module needs
        /// its compiled assembly - so that part waits for the reload. A delayed call, because on the
        /// reload itself the index and the assemblies are still settling; the generators wait the
        /// same way.
        /// </summary>
        [DidReloadScripts]
        private static void FinishAfterReload()
        {
            if (!EditorPrefs.HasKey(PENDING_KEY)) return;

            EditorApplication.delayCall += () => new ModuleRenameContinuation().Run();
        }

        private void RenameAssemblies(ModuleRenamePlanEVO plan, ModuleRenameReportEVO report)
        {
            foreach (AssemblyRenameEVO assembly in plan.Assemblies)
            {
                _files.Rewrite(assembly.AsmdefPath, text => _declared.Rename(text, assembly.OldName, assembly.NewName, out _));

                string folder = Path.GetDirectoryName(assembly.AsmdefPath) ?? string.Empty;

                Move(_paths.ToAssetPath(assembly.AsmdefPath), _paths.ToAssetPath(Path.Combine(folder, assembly.NewName + ASMDEF)));
                Log("Assembly " + assembly.OldName + " → " + assembly.NewName, report);
            }

            if (plan.Assemblies.Count == 0) return;

            foreach (string asmdef in FilesInScope(ASMDEF_PATTERN))
            {
                bool changed = _files.Rewrite(asmdef, text =>
                {
                    foreach (AssemblyRenameEVO assembly in plan.Assemblies)
                        text = _references.Rename(text, assembly.OldName, assembly.NewName, out _);

                    return text;
                });

                if (!changed) continue;

                report.AssemblyDefinitionsRewritten++;
                Log(Path.GetFileNameWithoutExtension(asmdef) + " references the new names", report);
            }
        }

        /// <summary>
        /// Every source file in scope, the module's own through the local rules and the rest through
        /// the global ones. The old paths hold: nothing has moved yet.
        /// </summary>
        private void RewriteSources(string modulePath, List<TextRule> global, List<TextRule> local, ModuleRenameReportEVO report)
        {
            string inside = Normalize(modulePath) + "/";

            foreach (string source in FilesInScope(SOURCE_PATTERN))
            {
                List<TextRule> rules = Normalize(source).StartsWith(inside, StringComparison.OrdinalIgnoreCase) ? local : global;

                if (_files.Rewrite(source, text => _rewriter.Rewrite(text, rules, out _)))
                    report.SourceFilesRewritten++;
            }

            Log(report.SourceFilesRewritten + " source file(s) rewritten", report);
        }

        private void RenameClassFiles(ModuleRenamePlanEVO plan, ModuleRenameReportEVO report)
        {
            foreach (ClassRenameEVO file in plan.Classes)
            {
                if (string.IsNullOrEmpty(file.NewPath)) continue;

                Move(_paths.ToAssetPath(file.Path), _paths.ToAssetPath(file.NewPath));
                Log(Path.GetFileName(file.Path) + " → " + Path.GetFileName(file.NewPath), report);
            }
        }

        private void RenameAssets(ModuleRenamePlanEVO plan, ModuleRenameReportEVO report)
        {
            foreach (AssetRenameEVO asset in plan.Assets)
            {
                Move(_paths.ToAssetPath(asset.Path), _paths.ToAssetPath(asset.NewPath));
                Log(Path.GetFileName(asset.Path) + " → " + Path.GetFileName(asset.NewPath), report);
            }

            foreach (ScreenAddressRenameEVO screen in plan.ScreenAddresses)
            {
                Log(new ScreenAddressables().Rename(
                        _paths.ToAssetPath(screen.NewPrefabPath),
                        _addresses.For(screen.OldAddress),
                        _addresses.For(screen.NewAddress)),
                    report);
            }
        }

        /// <summary>The first line only, and only when it is exactly the old name in one of its two shapes: the rest of the card is the author's.</summary>
        private void RenameCardHeadings(ModuleRenamePlanEVO plan, ModuleRenameReportEVO report)
        {
            foreach (ModuleRenameEVO module in plan.Modules)
            {
                string card = Path.Combine(module.OldPath, ModuleCardFile.FILE_NAME);

                if (!File.Exists(card)) continue;

                bool changed = _files.Rewrite(card, text =>
                    _headings.Renamed(text, module.OldName, module.NewName, module.OldStem, module.NewStem, out _));

                if (changed) Log(ModuleCardFile.FILE_NAME + " of " + module.OldName + " now heads " + module.NewName, report);
            }
        }

        /// <summary>
        /// Deepest first. The plan lists a parent before what it holds, and a follower's path is
        /// under its parent's old path - so the follower goes first, at the path the plan knows,
        /// and the parent after, taking the already-renamed follower with it.
        /// </summary>
        private void RenameFolders(ModuleRenamePlanEVO plan, ModuleRenameReportEVO report)
        {
            for (int index = plan.Modules.Count - 1; index >= 0; index--)
            {
                ModuleRenameEVO module = plan.Modules[index];

                Move(_paths.ToAssetPath(module.OldPath), _paths.ToAssetPath(NewPath(module)));
                Log("Folder " + module.OldName + " → " + module.NewName, report);
            }
        }

        private void RenameChannels(ModuleRenamePlanEVO plan, ModuleRenameReportEVO report)
        {
            CD_FlowConsole settings = FlowLogger.Settings;

            if (settings == null) return;

            foreach (ChannelRenameEVO channel in plan.Channels)
            {
                Log(settings.RenameLogType(channel.OldName, channel.NewName)
                        ? "Channel " + channel.OldName + " → " + channel.NewName
                        : "No channel named " + channel.OldName,
                    report);
            }
        }

        /// <summary>
        /// The Module Scanner's own writers, on the tree as it is now. The index goes first because
        /// the log type generator finds a module's folder through it; the settings files come off a
        /// fresh scan so their names are the new assemblies' and their paths the new folders'; the
        /// orphan sweep then takes the files named after assemblies that no longer exist.
        /// </summary>
        private void SettleProject(ModuleRenameEVO picked, ModuleRenameReportEVO report)
        {
            new ModuleIndexRebuilder().Rebuild();
            Log("Module index rebuilt", report);

            FlowLogTypeGenerator.Generate();
            Log("FlowLogType parts regenerated", report);

            (ProjectTargetEVO project, List<ModuleTargetEVO> modules) = new ModuleTargetFactory().Build();

            string inside = Normalize(NewPath(picked));
            var settings = new DotSettingsCheck();
            var written = 0;

            foreach (ModuleTargetEVO target in modules)
            {
                string path = Normalize(target.AbsolutePath);

                if (!path.Equals(inside, StringComparison.OrdinalIgnoreCase)
                    && !path.StartsWith(inside + "/", StringComparison.OrdinalIgnoreCase)) continue;

                settings.Fix(target);
                written++;
            }

            Log(written + " module(s) had their namespace settings written", report);

            new OrphanFilesCheck().Fix(project);
            Log("Orphaned project files removed", report);
        }

        private void Remember(ModuleRenamePlanEVO plan)
        {
            var pending = new PendingRenameEVO {Parent = plan.Picked.Row.Row.ParentName};

            foreach (ModuleRenameEVO module in plan.Modules)
                pending.Modules.Add(module.NewName);

            EditorPrefs.SetString(PENDING_KEY, JsonUtility.ToJson(pending));
        }

        /// <summary>
        /// Assets, and every module root inside an embedded package - where a module may live and
        /// name another. A folder ending in a tilde is not imported and is not in scope: the shipped
        /// payloads under Modules~ are republished, never edited.
        /// </summary>
        private IEnumerable<string> FilesInScope(string pattern)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string assets = Normalize(Application.dataPath);

            var roots = new List<string> {Application.dataPath};

            foreach (string root in new ModuleScannerRoots().All(projectRoot))
            {
                if (!Normalize(root).StartsWith(assets, StringComparison.OrdinalIgnoreCase)) roots.Add(root);
            }

            foreach (string root in roots)
            {
                if (!Directory.Exists(root)) continue;

                foreach (string path in Directory.EnumerateFiles(root, pattern, SearchOption.AllDirectories))
                {
                    if (Normalize(path).Split('/').Any(segment => segment.EndsWith("~", StringComparison.Ordinal))) continue;

                    yield return path;
                }
            }
        }

        private static string NewPath(ModuleRenameEVO module) =>
            Path.Combine(Path.GetDirectoryName(module.OldPath) ?? string.Empty, module.NewName);

        private static void Move(string fromAssetPath, string toAssetPath)
        {
            string error = AssetDatabase.MoveAsset(fromAssetPath, toAssetPath);

            if (!string.IsNullOrEmpty(error))
                throw new InvalidOperationException(fromAssetPath + " → " + toAssetPath + ": " + error);
        }

        private static void Log(string line, ModuleRenameReportEVO report)
        {
            Debug.Log(TAG + line);
            report.Lines.Add(line);
        }

        private static string Normalize(string path) => path.Replace('\\', '/').TrimEnd('/');
    }
}
#endif