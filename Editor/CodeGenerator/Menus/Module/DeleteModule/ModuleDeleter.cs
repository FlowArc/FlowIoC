#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using FlowIoC.BaseModule.ProjectPaths;
using FlowIoC.ConsoleModule;
using FlowIoC.Editor.Addressables;
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

            // Before the folder goes, and both of these read it: Addressables identifies an entry
            // by the GUID of an asset that still exists, so a screen unregistered afterwards cannot
            // be found at all, and the nested modules either pass is about are folders inside this
            // one.
            RemoveScreenAddressables(moduleName, modulePath, deletedItems);
            RemoveReferencesToModule(moduleName, modulePath, deletedItems);

            RemoveLogType(moduleName, modulePath, deletedItems);
            RemoveProjectFiles(moduleName, modulePath, deletedItems);
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
        /// Every module inside the folder is asked, not only the one being deleted: a main module
        /// holds screen modules and each of those was registered under a group named after itself.
        /// Asking about a module that is not a screen finds no group and reports nothing, which is
        /// cheaper than working out beforehand which of them to ask.
        /// </summary>
        private static void RemoveScreenAddressables(string moduleName, string modulePath, List<string> deletedItems)
        {
            foreach (string name in new ModuleNames().Of(modulePath, moduleName))
                RemoveScreenAddressable(name, deletedItems);
        }

        private static void RemoveScreenAddressable(string moduleName, List<string> deletedItems)
        {
            const string moduleSuffix = "Module";

            string screenName = moduleName.Substring(0, moduleName.Length - moduleSuffix.Length);

            ScreenAddressableEntry entry = new ScreenAddressableEntries().For(screenName);

            if (!new ScreenAddressables().Unregister(entry, out string removedGroup)) return;

            Log($"Addressable entry removed: {screenName}", deletedItems);

            if (!string.IsNullOrEmpty(removedGroup))
                Log($"Addressable group removed: {removedGroup}", deletedItems);
        }

        /// <summary>
        /// The module's assemblies, taken out of every asmdef that named them. Done before the
        /// deletion so the project is never in the state where a reference points at an assembly
        /// that has already gone.
        ///
        /// "The module's" means every asmdef under its folder rather than the three its name implies:
        /// a screen module holds a test module, a main module may hold several screen modules, and
        /// the assemblies those declare go with the folder like the rest of it.
        /// </summary>
        private static void RemoveReferencesToModule(
            string moduleName, string modulePath, List<string> deletedItems)
        {
            IReadOnlyList<string> assemblies = new ModuleAssemblies().Of(modulePath, moduleName);

            if (assemblies.Count == 0) return;

            foreach (string line in new ModuleReferenceCleaner().Clean(modulePath, assemblies))
                Log(line, deletedItems);
        }

        /// <summary>
        /// The FlowLogType channel of the module and of every module inside it. A nested module has
        /// a channel of its own, so a parent deleted without this leaves an empty column in the
        /// Filters panel that nothing will ever write to.
        ///
        /// ModuleAutoDetector removes an orphaned channel on its own, but only once per Editor
        /// session, so leaving it to that means the channel is wrong for as long as the Editor stays
        /// open. Doing it here also lets the deletion say which channels went, which is the report
        /// this whole method exists to write.
        /// </summary>
        private static void RemoveLogType(string moduleName, string modulePath, List<string> deletedItems)
        {
            CD_FlowConsole settings = FlowLogger.Settings;
            if (settings == null) return;

            foreach (string name in new ModuleNames().Of(modulePath, moduleName))
            {
                if (settings.RemoveLogType(name)) Log($"Log type removed: {name}", deletedItems);
            }
        }

        /// <summary>
        /// The project files the module left at the project root. Every assembly the module folder
        /// declares is a project of its own, so its `.csproj` and `.csproj.DotSettings` sit beside
        /// the module's and would otherwise outlive the module they belong to.
        ///
        /// Which assemblies those are is ModuleAssemblies' answer, read from the asmdefs inside the
        /// folder. This used to be the three the module's name implies, and a module that holds
        /// sub-modules declares more: deleting a screen module left its test module's two files at
        /// the root, which is what Module Scanner then reported as orphaned.
        /// </summary>
        private static void RemoveProjectFiles(string moduleName, string modulePath, List<string> deletedItems)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

            foreach (string assembly in new ModuleAssemblies().Of(modulePath, moduleName))
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