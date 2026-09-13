#if UNITY_EDITOR

using System.IO;
using FlowIoC.BaseModule.ProjectPaths;
using FlowIoC.Editor.CodeGenerator;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.Migration
{
    /// <summary>
    /// Moves a project that was set up by an older FlowIoC from the scattered layout
    /// (Assets/FlowIoC, Assets/Editor/FlowIoC, Assets/Resources) into the single root described by
    /// <see cref="FlowIoCProjectPaths"/>, keeping the user's folder colors and generator
    /// configuration.
    ///
    /// Callers must be on an editor tick where the AssetDatabase is writable - after a delayCall,
    /// after an isUpdating/isCompiling guard, or from a menu action. Running straight out of
    /// [InitializeOnLoadMethod] is not safe, which is why the bootstrap defers.
    /// </summary>
    internal class FlowIoCPathMigrator
    {
        private const string CompletedKey = "FlowIoC_PathMigration_Completed";

        internal void MigrateIfNeeded()
        {
            var paths = new FlowIoCProjectPaths();
            var legacyPaths = new FlowIoCLegacyPaths(paths);

            // Not once per session. The session flag below was set by whatever FlowIoC was loaded
            // when the session began, and a package upgraded under an open Editor is exactly the
            // case this has to catch - so the retirements are gated by what is on disk instead,
            // which is a handful of File.Exists once nothing is left.
            RetireIfNeeded(legacyPaths);

            if (SessionState.GetBool(CompletedKey, false)) return;
            SessionState.SetBool(CompletedKey, true);

            Migrate(paths, legacyPaths);
        }

        private void Migrate(FlowIoCProjectPaths paths, FlowIoCLegacyPaths legacyPaths)
        {
            bool movedAnything = MoveLegacyAssets(legacyPaths);
            if (!movedAnything) return;

            RewriteDirectoryStructureConfigPaths(paths);
            CleanUpLegacyFolders(legacyPaths);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=cyan>FlowIoC:</color> project assets were moved to {paths.Root}.");
        }

        /// <summary>
        /// What an older FlowIoC left that this one has no use for: the sources saying FlowLogType,
        /// and the assets under <see cref="FlowIoCLegacyPaths.AssetsToDelete"/>. Sources first -
        /// once nothing says the old name, the files that declared it are orphans and can go; the
        /// module parts among them go on the generator's sweep, which runs this on the same locked
        /// pass before it writes the new ones.
        /// </summary>
        private void RetireIfNeeded(FlowIoCLegacyPaths legacyPaths)
        {
            var references = new FlowModuleReferenceMigrator();

            bool rewroteAnything = references.IsNeeded() && RewriteChannelReferences(references);
            bool deletedAnything = DeleteRetiredAssets(legacyPaths);
            if (!rewroteAnything && !deletedAnything) return;

            CleanUpLegacyFolders(legacyPaths);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// FlowLogType became FlowModule when the constants started naming the module rather than
        /// the log. Every source that says the old name is rewritten, and said in the console: a
        /// diff across the whole project deserves a sentence about where it came from.
        /// </summary>
        private static bool RewriteChannelReferences(FlowModuleReferenceMigrator references)
        {
            int rewritten = references.RewriteProject();
            if (rewritten == 0) return false;

            Debug.Log(
                $"<color=cyan>FlowIoC:</color> {rewritten} source file(s) now say {FlowModuleReferenceMigrator.NEW_NAME} "
                + $"where they said {FlowModuleReferenceMigrator.OLD_NAME}: the constants name the module, not a log type, "
                + "and the class was renamed to say so.");

            return true;
        }

        /// <summary>
        /// An asset FlowIoC has no use for any more goes rather than moves. Said in the console,
        /// because a file disappearing from a tracked tree deserves a sentence about why.
        /// </summary>
        private bool DeleteRetiredAssets(FlowIoCLegacyPaths legacyPaths)
        {
            bool deletedAnything = false;

            foreach (string assetPath in legacyPaths.AssetsToDelete)
            {
                if (!File.Exists(ToDiskPath(assetPath))) continue;

                if (!AssetDatabase.DeleteAsset(assetPath))
                {
                    Debug.LogError($"<color=cyan>FlowIoC:</color> could not delete {assetPath}.");
                    continue;
                }

                deletedAnything = true;
                Debug.Log(
                    $"<color=cyan>FlowIoC:</color> {assetPath} was removed. The Flow Console keeps a developer's "
                    + "settings in EditorPrefs and a module's colour in the module now, so the asset had "
                    + "nothing left to hold.");
            }

            return deletedAnything;
        }

        private bool MoveLegacyAssets(FlowIoCLegacyPaths legacyPaths)
        {
            var movePolicy = new LegacyAssetMovePolicy();
            bool movedAnything = false;

            foreach (LegacyAssetMove move in legacyPaths.AssetMoves)
            {
                bool legacyExists = File.Exists(ToDiskPath(move.Legacy));
                bool destinationExists = File.Exists(ToDiskPath(move.Destination));

                if (!movePolicy.ShouldMove(legacyExists, destinationExists))
                {
                    if (legacyExists && destinationExists)
                    {
                        Debug.LogWarning(
                            $"<color=cyan>FlowIoC:</color> {move.Legacy} was left where it is, because " +
                            $"{move.Destination} already exists. Keep whichever copy holds your settings " +
                            "and delete the other one by hand.");
                    }

                    continue;
                }

                EnsureFolder(ParentFolderOf(move.Destination));

                string error = AssetDatabase.MoveAsset(move.Legacy, move.Destination);
                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogError(
                        $"<color=cyan>FlowIoC:</color> could not move {move.Legacy} to " +
                        $"{move.Destination}: {error}");
                    continue;
                }

                movedAnything = true;
            }

            return movedAnything;
        }

        /// <summary>
        /// The three directory structure config paths are serialized strings inside the settings
        /// asset, so updating the defaults in code does not reach a project that already has one.
        /// </summary>
        private void RewriteDirectoryStructureConfigPaths(FlowIoCProjectPaths paths)
        {
            var settings = AssetDatabase.LoadAssetAtPath<ED_CodeGenerator>(paths.CodeGeneratorSettings);
            if (settings == null || settings.DirectoryStructureConfigPaths == null) return;

            foreach (string configKey in new[] {"Main", "Screen", "Test"})
            {
                if (!settings.DirectoryStructureConfigPaths.ContainsKey(configKey)) continue;

                settings.DirectoryStructureConfigPaths[configKey] = paths.DirectoryStructureConfig(configKey);
            }

            EditorUtility.SetDirty(settings);
        }

        private void CleanUpLegacyFolders(FlowIoCLegacyPaths legacyPaths)
        {
            var cleanupPolicy = new LegacyFolderCleanupPolicy();

            foreach (string folder in legacyPaths.FoldersToCleanUp)
            {
                string diskPath = ToDiskPath(folder);
                bool exists = Directory.Exists(diskPath);
                bool isEmpty = exists && Directory.GetFileSystemEntries(diskPath).Length == 0;

                if (!cleanupPolicy.ShouldDelete(exists, isEmpty)) continue;

                AssetDatabase.DeleteAsset(folder);
            }
        }

        private void EnsureFolder(string assetFolderPath)
        {
            if (string.IsNullOrEmpty(assetFolderPath)) return;
            if (AssetDatabase.IsValidFolder(assetFolderPath)) return;

            string parent = ParentFolderOf(assetFolderPath);
            EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, Path.GetFileName(assetFolderPath));
        }

        private string ParentFolderOf(string assetPath)
        {
            string parent = Path.GetDirectoryName(assetPath);
            return parent == null ? null : parent.Replace("\\", "/");
        }

        private string ToDiskPath(string assetPath)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.Combine(projectRoot, assetPath);
        }
    }
}

#endif