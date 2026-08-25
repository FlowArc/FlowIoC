#if UNITY_EDITOR
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
        public void Register(
            string modulePath,
            DirectoryStructureConfig directoryConfig,
            IEnumerable<FolderConfig.FolderType> folderTypes)
        {
            new ModuleIndexRebuilder().Rebuild();

            FlowIoCModuleIndex index = new ModuleIndexProvider().LoadOrCreate();

            string moduleGuid = AssetDatabase.AssetPathToGUID(NamespaceUtility.GetUnityAssetPath(modulePath));
            if (string.IsNullOrEmpty(moduleGuid) || !index.TryGetByFolderGuid(moduleGuid, out ModuleDescriptor descriptor))
                return;

            foreach (FolderConfig.FolderType type in folderTypes)
            {
                string folderPath = directoryConfig.FindFullFolderPathByID(type, modulePath);
                if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath)) continue;

                string guid = AssetDatabase.AssetPathToGUID(NamespaceUtility.GetUnityAssetPath(folderPath));
                if (string.IsNullOrEmpty(guid)) continue;

                descriptor.RecordFolderGuid(type, guid);
            }

            EditorUtility.SetDirty(index);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
