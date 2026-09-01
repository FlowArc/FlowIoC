#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using FlowIoC.BaseModule.ProjectPaths;
using FlowIoC.Editor.AgentRules;
using FlowIoC.Editor.CodeGenerator;
using FlowIoC.Editor.CodeGenerator.Menus.Module;
using FlowIoC.Editor.Config.ModuleConfig;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.Modules
{
    /// <summary>
    /// Holds the one instance Unity's load callback needs. Unity forces this entry point to be
    /// static; everything it does lives on <see cref="MarkerFileMigration"/>.
    ///
    /// Deferred through <see cref="EditorApplication.update"/> with an isUpdating/isCompiling
    /// guard rather than <see cref="EditorApplication.delayCall"/>, the same way
    /// <c>FlowIoCPathMigrationBootstrap</c> defers <c>FlowIoCPathMigrator</c>: <c>Run</c> reaches
    /// <c>AssetDatabase.CreateAsset</c> (through <c>ModuleIndexProvider.LoadOrCreate</c> and
    /// <c>GetOrCreateConfig</c>), which delayCall does not guarantee is safe to call - it fires on
    /// the next editor tick regardless of whether the AssetDatabase is still importing or the
    /// project is still compiling.
    /// </summary>
    internal static class MarkerFileMigrationHook
    {
        [InitializeOnLoadMethod]
        private static void OnLoad()
        {
            EditorApplication.update -= Run;
            EditorApplication.update += Run;
        }

        private static void Run()
        {
            if (EditorApplication.isUpdating || EditorApplication.isCompiling) return;

            EditorApplication.update -= Run;

            new MarkerFileMigration().Run();
        }
    }

    /// <summary>
    /// Remembers that the marker migration already ran for a project. EditorPrefs is shared by
    /// every project a user opens with the same Editor, so the key carries the project root the
    /// same way <see cref="AgentRulesDismissal"/> does - completing the migration in one project
    /// must not silently skip it in another.
    /// </summary>
    internal class MarkerFileMigrationGuard
    {
        private const string KeyPrefix = "FlowIoC.MarkerMigration.Completed.";

        internal string KeyFor(string projectRoot)
        {
            string normalized = (projectRoot ?? string.Empty)
                .Replace('\\', '/')
                .TrimEnd('/')
                .ToLowerInvariant();

            return KeyPrefix + new ManagedBlockWriter().ComputeHash(normalized);
        }

        internal bool HasRun(string projectRoot) => EditorPrefs.GetBool(KeyFor(projectRoot), false);

        internal void MarkRun(string projectRoot) => EditorPrefs.SetBool(KeyFor(projectRoot), true);

        internal void Clear(string projectRoot) => EditorPrefs.DeleteKey(KeyFor(projectRoot));
    }

    /// <summary>
    /// The eager counterpart to <c>ED_CodeGenerator</c>' lazy rename-healing fallback: this
    /// walks every module the index already knows about, rather than waiting for someone to
    /// rename a folder type in the settings. The one rule that keeps it safe to run more than
    /// once is that a folder GUID already recorded is never touched - it was written by
    /// <c>ModuleIndexRegistrar</c> at the moment generation laid the folder down, or by an
    /// earlier pass of this same backfill, and either is more trustworthy than a name lookup
    /// made after the fact.
    /// </summary>
    internal class ModuleFolderGuidBackfiller
    {
        /// <summary>
        /// For every module and every folder type, <paramref name="resolveFolderGuid"/> is asked
        /// for a GUID only when the module has no GUID recorded for that type yet - and only its
        /// answer, never an existing recording, is written back. Returns how many were newly
        /// recorded.
        /// </summary>
        internal int Backfill(
            IEnumerable<ModuleDescriptorEVO> modules,
            IEnumerable<FolderEVO.FolderType> types,
            Func<ModuleDescriptorEVO, FolderEVO.FolderType, string> resolveFolderGuid)
        {
            int recorded = 0;
            if (modules == null || types == null || resolveFolderGuid == null) return recorded;

            var typeList = new List<FolderEVO.FolderType>(types);

            foreach (ModuleDescriptorEVO module in modules)
            {
                if (module == null) continue;

                foreach (FolderEVO.FolderType type in typeList)
                {
                    if (module.TryGetFolderGuid(type, out _)) continue;

                    string guid = resolveFolderGuid(module, type);
                    if (string.IsNullOrEmpty(guid)) continue;

                    module.RecordFolderGuid(type, guid);
                    recorded++;
                }
            }

            return recorded;
        }
    }

    /// <summary>
    /// Runs once per project: backfills every module's folder GUIDs while the marker files that
    /// used to be the only physical trace of a pre-existing module's folders are still on disk,
    /// then deletes them. Sweeping first would risk leaving a project with neither the markers
    /// nor a recorded GUID if the Editor closed mid-migration, so the backfill - and the save
    /// that makes it durable - always runs to completion before the sweep starts.
    /// </summary>
    internal class MarkerFileMigration
    {
        private readonly MarkerFileMigrationGuard _guard = new MarkerFileMigrationGuard();

        internal void Run()
        {
            // A headless run has nobody to show progress to and no interactive session worth
            // guarding against; skip it rather than mutate a CI checkout.
            if (Application.isBatchMode) return;

            string projectRoot = new ProjectRoot().Resolve();
            if (_guard.HasRun(projectRoot)) return;

            BackfillFolderGuids();

            // The deletion is the one step that must never repeat, so the guard is armed
            // immediately after it - not after the refresh and rebuild that follow, which are
            // both idempotent and safe to redo if the domain tears down before they finish.
            List<string> deleted = new MarkerFileSweeper().Sweep(Application.dataPath);
            _guard.MarkRun(projectRoot);

            AssetDatabase.Refresh();
            new ModuleIndexRebuilder().Rebuild();

            if (deleted.Count > 0)
            {
                Debug.Log($"<color=cyan>FlowIoC:</color> the legacy marker migration removed " +
                          $"{deleted.Count} marker file(s).");
            }
        }

        /// <summary>
        /// Steps 1-4 of the migration on their own: rebuild so every module on disk has a
        /// descriptor, then record the folder GUID of every <see cref="FolderEVO.FolderType"/>
        /// a module already has - found by its configured name under that module, the way
        /// <c>ModuleIndexRegistrar</c> resolves one module's folders at creation time - without
        /// overwriting a GUID that is already there. Returns how many were newly recorded.
        ///
        /// Exposed separately from <see cref="Run"/> so the backfill, which only ever records
        /// GUIDs and never touches a file, can be exercised against a real project without also
        /// running the sweep.
        /// </summary>
        internal int BackfillFolderGuids()
        {
            ED_ModuleIndex index = new ModuleIndexRebuilder().Rebuild();
            if (index == null) return 0;

            IAssetPaths assetPaths = new AssetDatabasePaths();
            var registry = new ModuleRegistry(index, assetPaths);
            var pathResolver = new ModuleAssetPathResolver();

            ED_CodeGenerator settings =
                AssetDatabase.LoadAssetAtPath<ED_CodeGenerator>(new FlowIoCProjectPaths().CodeGeneratorSettings);

            if (settings == null)
            {
                Debug.LogWarning("<color=cyan>FlowIoC:</color> the code generator settings could not be " +
                                 "loaded, so no folder GUIDs were backfilled.");
                return 0;
            }

            string dataPath = Application.dataPath.Replace('\\', '/');

            // Resolved lazily, per config key, the first time a module of that kind is actually
            // encountered - not eagerly for all four kinds up front. GetOrCreateConfig throws
            // when DirectoryStructureConfigPaths has no entry for the key, which
            // ED_CodeGenerator.cs documents as a real hazard on a project whose settings
            // asset predates a later FlowIoC version; a project with only Main modules must
            // never construct, or fail on, Screen/Test configs it will never use. Sub modules
            // share the Main directory structure, the same mapping
            // ED_CodeGenerator.CollectFolderOperations uses for its own, later, lazier pass
            // over the same folder types.
            var directoryConfigByKey = new Dictionary<string, DirectoryStructureConfig>();

            var configProvider = new DirectoryStructureConfigProvider();

            DirectoryStructureConfig ResolveDirectoryConfig(ModuleKind kind)
            {
                string configKey = configProvider.ConfigKeyOf(kind);

                if (directoryConfigByKey.TryGetValue(configKey, out DirectoryStructureConfig cached)) return cached;

                if (settings.DirectoryStructureConfigPaths == null || !settings.DirectoryStructureConfigPaths.ContainsKey(configKey))
                {
                    Debug.LogWarning($"<color=cyan>FlowIoC:</color> the code generator settings have no " +
                                     $"'{configKey}' directory structure config path recorded, so folder " +
                                     "GUIDs were not backfilled for its modules.");
                    directoryConfigByKey[configKey] = null;
                    return null;
                }

                DirectoryStructureConfig resolved = configProvider.ConfigFor(kind);

                directoryConfigByKey[configKey] = resolved;
                return resolved;
            }

            string ResolveFolderGuid(ModuleDescriptorEVO module, FolderEVO.FolderType type)
            {
                DirectoryStructureConfig directoryConfig = ResolveDirectoryConfig(module.Kind);
                if (directoryConfig == null) return string.Empty;

                string moduleAssetPath = registry.PathOf(module);
                if (string.IsNullOrEmpty(moduleAssetPath)) return string.Empty;

                string moduleAbsolutePath = pathResolver.ToAbsolutePath(moduleAssetPath);
                if (string.IsNullOrEmpty(moduleAbsolutePath)) return string.Empty;

                string folderPath = directoryConfig.FindFullFolderPathByID(type, moduleAbsolutePath);
                if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath)) return string.Empty;

                // NamespaceUtility.GetUnityAssetPath only resolves paths under Assets/. An
                // embedded-package module sits outside it; ED_CodeGenerator' own fallback
                // draws the same boundary around this exact case, so this leaves it alone too
                // rather than risk recording a GUID for the wrong folder.
                if (!folderPath.Replace('\\', '/').StartsWith(dataPath, StringComparison.Ordinal))
                    return string.Empty;

                string assetPath = NamespaceUtility.GetUnityAssetPath(folderPath);
                return assetPaths.GuidOf(assetPath);
            }

            int recorded = new ModuleFolderGuidBackfiller().Backfill(
                index.Modules, settings.DirectoryStructureConfigMap.Keys, ResolveFolderGuid);

            EditorUtility.SetDirty(index);
            AssetDatabase.SaveAssets();

            return recorded;
        }
    }
}
#endif