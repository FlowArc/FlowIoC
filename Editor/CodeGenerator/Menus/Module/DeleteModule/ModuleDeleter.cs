#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using FlowIoC.BaseModule.ProjectPaths;
using FlowIoC.ConsoleModule;
using FlowIoC.Editor.Addressables;
using FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration;
using FlowIoC.Editor.Config.ModuleConfig;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.DeleteModule
{
    internal static class ModuleDeleter
    {
        /// <summary>
        /// Deletes the module and hands back what was removed, one line per item.
        ///
        /// Nothing here opens a dialog. A modal dialog is the window's job, and putting one at the
        /// end of this method would make the deleter unusable from anything without a user in front
        /// of it - a batch script, an editor test, an agent driving the Editor - because a modal
        /// blocks the Editor until somebody clicks it.
        /// </summary>
        public static IReadOnlyList<string> DeleteModule(string moduleName, string modulePath, string folderGuid)
        {
            var deletedItems = new List<string>();

            Debug.Log($"<color=cyan>[ModuleDeleter]</color> Deleting module '{moduleName}'...");

            // Before the folder goes: Addressables identifies an entry by the GUID of an asset
            // that still exists, so a screen unregistered afterwards cannot be found at all.
            RemoveScreenAddressables(moduleName, deletedItems);
            RemoveReferencesToModule(moduleName, modulePath, deletedItems);

            RemoveLogType(moduleName, deletedItems);
            RemoveProjectFiles(moduleName, deletedItems);
            DeleteModuleFolder(modulePath, deletedItems);
            CleanupEmptyParentFolder(modulePath, deletedItems);
            RemoveFromIndex(folderGuid, deletedItems);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=cyan>[ModuleDeleter]</color> Deleted module '{moduleName}':\n"
                      + string.Join("\n", deletedItems));

            return deletedItems;
        }

        private static void Log(string message, List<string> deletedItems)
        {
            Debug.Log($"<color=cyan>[ModuleDeleter]</color> {message}");
            deletedItems.Add(message);
        }

        private static void DeleteModuleFolder(string modulePath, List<string> deletedItems)
        {
            if (!Directory.Exists(modulePath)) return;

            string assetPath = GetUnityAssetPath(modulePath);

            if (!string.IsNullOrEmpty(assetPath) && AssetDatabase.IsValidFolder(assetPath))
            {
                AssetDatabase.DeleteAsset(assetPath);
                Log($"Folder deleted: {assetPath}", deletedItems);
            }
            else
            {
                Directory.Delete(modulePath, true);

                string metaFile = modulePath + ".meta";
                if (File.Exists(metaFile))
                    File.Delete(metaFile);

                Log($"Folder deleted: {modulePath}", deletedItems);
            }
        }

        private static void CleanupEmptyParentFolder(string modulePath, List<string> deletedItems)
        {
            string parentPath = Path.GetDirectoryName(modulePath);
            if (string.IsNullOrEmpty(parentPath)) return;

            string parentName = Path.GetFileName(parentPath);
            if (!IsModuleContainerFolder(parentName)) return;

            if (!Directory.Exists(parentPath)) return;

            string[] remaining = Directory.GetFileSystemEntries(parentPath);
            if (remaining.Length > 0) return;

            string assetPath = GetUnityAssetPath(parentPath);
            if (!string.IsNullOrEmpty(assetPath) && AssetDatabase.IsValidFolder(assetPath))
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
            else
            {
                Directory.Delete(parentPath);
                string metaFile = parentPath + ".meta";
                if (File.Exists(metaFile))
                    File.Delete(metaFile);
            }

            Log($"Empty parent folder deleted: {parentName}", deletedItems);
        }

        /// <summary>
        /// The three container folder names are configurable, and this used to test for the
        /// hardcoded "zSub" / "zTest" / "zScreen" prefixes instead: renaming zSubModules in the
        /// code generator settings left the emptied container behind after the last sub-module in
        /// it was deleted. The hardcoded names stay only as the fallback for a project whose
        /// settings asset cannot be loaded.
        /// </summary>
        private static bool IsModuleContainerFolder(string folderName)
        {
            ED_CodeGenerator settings = AssetDatabase.LoadAssetAtPath<ED_CodeGenerator>(
                new FlowIoCProjectPaths().CodeGeneratorSettings);

            string[] containerNames = settings == null
                ? new[] {"zSubModules", "zTestModules", "zScreenModules"}
                : new[]
                {
                    settings.FolderNameFor(FolderEVO.FolderType.SubModules, "zSubModules"),
                    settings.FolderNameFor(FolderEVO.FolderType.TestModules, "zTestModules"),
                    settings.FolderNameFor(FolderEVO.FolderType.ScreenModules, "zScreenModules")
                };

            foreach (string containerName in containerNames)
            {
                if (string.Equals(folderName, containerName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static void RemoveFromIndex(string folderGuid, List<string> deletedItems)
        {
            if (string.IsNullOrEmpty(folderGuid)) return;

            new ModuleIndexDeregistrar().Deregister(folderGuid);
            Log("Removed from module index", deletedItems);
        }

        /// <summary>
        /// The Addressables registration a screen module was created with. Create Module makes the
        /// screen prefab addressable in a group of its own, so deleting the module without this
        /// leaves an empty Local_Screen- group and its schema assets behind - which is what the
        /// project then carries around, unread by anything.
        ///
        /// Only a screen module has one. Asking for any other module finds no group and reports
        /// nothing, which is cheaper than working out beforehand whether to ask.
        /// </summary>
        private static void RemoveScreenAddressables(string moduleName, List<string> deletedItems)
        {
            const string moduleSuffix = "Module";

            if (!moduleName.EndsWith(moduleSuffix, StringComparison.Ordinal)) return;

            string screenName = moduleName.Substring(0, moduleName.Length - moduleSuffix.Length);

            ScreenAddressableEntry entry = new ScreenAddressableEntries().For(screenName);

            if (!new ScreenAddressables().Unregister(entry, out string removedGroup)) return;

            Log($"Addressable entry removed: {screenName}", deletedItems);

            if (!string.IsNullOrEmpty(removedGroup))
                Log($"Addressable group removed: {removedGroup}", deletedItems);
        }

        /// <summary>
        /// The module's three assemblies, taken out of every asmdef that named them. Done before
        /// the deletion so the project is never in the state where a reference points at an
        /// assembly that has already gone.
        /// </summary>
        private static void RemoveReferencesToModule(
            string moduleName, string modulePath, List<string> deletedItems)
        {
            string assemblyName = new ModuleAssemblyName().From(moduleName);

            if (string.IsNullOrEmpty(assemblyName)) return;

            var assemblies = new List<string>
            {
                assemblyName,
                assemblyName + SharedAssemblyDefinition.ASSEMBLY_SUFFIX,
                assemblyName + SignalsAssemblyDefinition.ASSEMBLY_SUFFIX
            };

            foreach (string line in new ModuleReferenceCleaner().Clean(modulePath, assemblies))
                Log(line, deletedItems);
        }

        private static void RemoveLogType(string moduleName, List<string> deletedItems)
        {
            var settings = FlowLogger.Settings;
            if (settings == null) return;

            if (settings.RemoveLogType(moduleName))
            {
                Log($"Log type removed: {moduleName}", deletedItems);
            }
        }

        /// <summary>
        /// The project files the module left at the project root. A module carves two more
        /// assemblies out of itself - Shared for the data it publishes and Signals for its public
        /// holder - and each is a project of its own, so its `.csproj` and `.csproj.DotSettings`
        /// sit beside the module's and would otherwise outlive the module they belong to.
        /// </summary>
        private static void RemoveProjectFiles(string moduleName, List<string> deletedItems)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

            // Asked of ModuleAssemblyName rather than worked out here. This method used to carry
            // its own copy of the rules, and the copy read the suffix off the end without asking
            // what was left in front of it: "GameplayScreenTestModule" came out as
            // Modules.GameplayScreen.Test instead of Modules.Gameplay.Screen.Test, and a module
            // called exactly "ScreenModule" came out as "Modules..Screen". Neither name matched a
            // file, so nothing was deleted and the module's settings files outlived it - which is
            // what Module Scanner then reported as orphaned.
            string assemblyName = new ModuleAssemblyName().From(moduleName);

            foreach (string assembly in new[]
                     {
                         assemblyName,
                         assemblyName + SharedAssemblyDefinition.ASSEMBLY_SUFFIX,
                         assemblyName + SignalsAssemblyDefinition.ASSEMBLY_SUFFIX
                     })
            {
                RemoveProjectFile(projectRoot, assembly, ".csproj.DotSettings", "DotSettings", deletedItems);
                RemoveProjectFile(projectRoot, assembly, ".csproj", "Csproj", deletedItems);
            }
        }

        private static void RemoveProjectFile(
            string projectRoot, string assemblyName, string extension, string label, List<string> deletedItems)
        {
            string path = Path.Combine(projectRoot, assemblyName + extension);

            if (!File.Exists(path)) return;

            File.Delete(path);
            Log($"{label} deleted: {assemblyName}{extension}", deletedItems);
        }

        private static string GetUnityAssetPath(string absolutePath)
        {
            string normalized = absolutePath.Replace('\\', '/');
            int assetsIdx = normalized.IndexOf("/Assets/");
            if (assetsIdx >= 0)
                return normalized.Substring(assetsIdx + 1);
            return null;
        }
    }
}
#endif