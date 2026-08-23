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
    /// <see cref="FlowIoCProjectPaths"/>, keeping the user's log types, folder colors and generator
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
            if (SessionState.GetBool(CompletedKey, false)) return;
            SessionState.SetBool(CompletedKey, true);

            Migrate();
        }

        private void Migrate()
        {
            var paths = new FlowIoCProjectPaths();
            var legacyPaths = new FlowIoCLegacyPaths(paths);

            bool movedAnything = MoveLegacyAssets(legacyPaths);
            if (!movedAnything) return;

            RewriteDirectoryStructureConfigPaths(paths);
            CleanUpLegacyFolders(legacyPaths);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=cyan>FlowIoC:</color> project assets were moved to {paths.Root}.");
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
            var settings = AssetDatabase.LoadAssetAtPath<CodeGeneratorSettings>(paths.CodeGeneratorSettings);
            if (settings == null || settings.DirectoryStructureConfigPaths == null) return;

            foreach (string configKey in new[] { "Main", "Screen", "Test" })
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
