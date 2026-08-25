#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using FlowIoC.BaseModule.ProjectPaths;
using FlowIoC.ConsoleModule;
using FlowIoC.Editor.Config.ModuleConfig;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.DeleteModule
{
    internal static class ModuleDeleter
    {
        public static void DeleteModule(string moduleName, string modulePath, string folderGuid)
        {
            var deletedItems = new List<string>();

            Debug.Log($"<color=cyan>[ModuleDeleter]</color> Deleting module '{moduleName}'...");

            RemoveLogType(moduleName, deletedItems);
            RemoveDotSettingsFile(moduleName, deletedItems);
            RemoveCsprojFile(moduleName, deletedItems);
            DeleteModuleFolder(modulePath, deletedItems);
            CleanupEmptyParentFolder(modulePath, deletedItems);
            RemoveFromIndex(folderGuid, deletedItems);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string summary = string.Join("\n", deletedItems);
            EditorUtility.DisplayDialog(
                "Module Deleted",
                $"'{moduleName}' has been deleted.\n\n{summary}",
                "OK");

            Debug.Log($"<color=cyan>[ModuleDeleter]</color> Deleted module '{moduleName}':\n{summary}");
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
            CodeGeneratorSettings settings = AssetDatabase.LoadAssetAtPath<CodeGeneratorSettings>(
                new FlowIoCProjectPaths().CodeGeneratorSettings);

            string[] containerNames = settings == null
                ? new[] {"zSubModules", "zTestModules", "zScreenModules"}
                : new[]
                {
                    settings.FolderNameFor(FolderConfig.FolderType.SubModules, "zSubModules"),
                    settings.FolderNameFor(FolderConfig.FolderType.TestModules, "zTestModules"),
                    settings.FolderNameFor(FolderConfig.FolderType.ScreenModules, "zScreenModules")
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

        private static void RemoveLogType(string moduleName, List<string> deletedItems)
        {
            var settings = FlowLogger.Settings;
            if (settings == null) return;

            if (settings.RemoveLogType(moduleName))
            {
                Log($"Log type removed: {moduleName}", deletedItems);
            }
        }

        private static void RemoveDotSettingsFile(string moduleName, List<string> deletedItems)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string assemblyName = ConvertToAssemblyName(moduleName);
            string dotSettingsPath = Path.Combine(projectRoot, assemblyName + ".csproj.DotSettings");

            if (File.Exists(dotSettingsPath))
            {
                File.Delete(dotSettingsPath);
                Log($"DotSettings deleted: {assemblyName}.csproj.DotSettings", deletedItems);
            }
        }

        private static void RemoveCsprojFile(string moduleName, List<string> deletedItems)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string assemblyName = ConvertToAssemblyName(moduleName);
            string csprojPath = Path.Combine(projectRoot, assemblyName + ".csproj");

            if (File.Exists(csprojPath))
            {
                File.Delete(csprojPath);
                Log($"Csproj deleted: {assemblyName}.csproj", deletedItems);
            }
        }

        private static string ConvertToAssemblyName(string rawName)
        {
            const string prefix = "Modules.";

            if (rawName.EndsWith("ScreenModule", StringComparison.OrdinalIgnoreCase))
            {
                string coreName = rawName.Substring(0, rawName.Length - "ScreenModule".Length);
                return prefix + coreName + ".Screen";
            }

            if (rawName.EndsWith("TestModule", StringComparison.OrdinalIgnoreCase))
            {
                string coreName = rawName.Substring(0, rawName.Length - "TestModule".Length);
                return prefix + coreName + ".Test";
            }

            if (rawName.EndsWith("Module", StringComparison.OrdinalIgnoreCase))
            {
                string coreName = rawName.Substring(0, rawName.Length - "Module".Length);
                return prefix + coreName;
            }

            return prefix + rawName;
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