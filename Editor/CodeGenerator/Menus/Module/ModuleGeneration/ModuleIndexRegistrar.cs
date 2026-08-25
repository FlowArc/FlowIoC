#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.Config.ModuleConfig;
using FlowIoC.Editor.Modules;
using UnityEditor;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration
{
    /// <summary>
    /// What ProcessLockedFoldersRecursively used to do by writing a marker file into every
    /// locked folder it found. A rebuild can recover a module's own entry by rescanning the
    /// folder tree, but it cannot recover which folder on disk corresponds to which
    /// FolderConfig.FolderType - that mapping only exists at the moment generation itself lays
    /// the folder down, which is why this runs from ModuleGenerator right after the folders it
    /// is about to record get their Unity GUIDs.
    /// </summary>
    internal class ModuleIndexRegistrar
    {
        private readonly IAssetPaths _assetPaths;

        public ModuleIndexRegistrar() : this(new AssetDatabasePaths())
        {
        }

        /// <summary>
        /// This writes the only index data a rebuild cannot regenerate, so it goes through the
        /// IAssetPaths seam the rest of the module code uses rather than calling AssetDatabase
        /// directly - which is what lets <see cref="RecordFolderGuids"/> be exercised without a
        /// live Editor.
        /// </summary>
        internal ModuleIndexRegistrar(IAssetPaths assetPaths)
        {
            _assetPaths = assetPaths;
        }

        public void Register(
            string modulePath,
            DirectoryStructureConfig directoryConfig,
            IEnumerable<FolderConfig.FolderType> folderTypes)
        {
            // Without the rebuild the new module has no descriptor to record folder GUIDs on, and
            // an index loaded independently would hand back an empty one - so the lookup below
            // would miss and the mapping a rebuild cannot regenerate would be lost silently.
            FlowIoCModuleIndex index = new ModuleIndexRebuilder().Rebuild();
            if (index == null) return;

            string ResolveFolderAssetPath(FolderConfig.FolderType type)
            {
                string folderPath = directoryConfig.FindFullFolderPathByID(type, modulePath);
                if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath)) return string.Empty;

                return NamespaceUtility.GetUnityAssetPath(folderPath);
            }

            bool recorded = RecordFolderGuids(
                index, NamespaceUtility.GetUnityAssetPath(modulePath), folderTypes, ResolveFolderAssetPath);

            if (!recorded) return;

            EditorUtility.SetDirty(index);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Records the GUID of every folder <paramref name="resolveFolderAssetPath"/> can name for
        /// the module at <paramref name="moduleAssetPath"/>. Returns false when the module itself
        /// is not in the index, which is the one case where there is nothing to write and nothing
        /// to save.
        ///
        /// Folder path resolution is a parameter rather than a call into DirectoryStructureConfig
        /// here, because that walk ends in a Directory.Exists check and this method is the part
        /// that has to stay testable without folders on disk.
        /// </summary>
        internal bool RecordFolderGuids(
            FlowIoCModuleIndex index,
            string moduleAssetPath,
            IEnumerable<FolderConfig.FolderType> folderTypes,
            Func<FolderConfig.FolderType, string> resolveFolderAssetPath)
        {
            if (index == null || folderTypes == null || resolveFolderAssetPath == null) return false;

            string moduleGuid = _assetPaths.GuidOf(moduleAssetPath);
            if (string.IsNullOrEmpty(moduleGuid) || !index.TryGetByFolderGuid(moduleGuid, out ModuleDescriptor descriptor))
                return false;

            foreach (FolderConfig.FolderType type in folderTypes)
            {
                string folderAssetPath = resolveFolderAssetPath(type);
                if (string.IsNullOrEmpty(folderAssetPath)) continue;

                string guid = _assetPaths.GuidOf(folderAssetPath);
                if (string.IsNullOrEmpty(guid)) continue;

                descriptor.RecordFolderGuid(type, guid);
            }

            return true;
        }
    }
}
#endif