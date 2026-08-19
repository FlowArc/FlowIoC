#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using FlowIoC.Editor.CodeGenerator.Menus.Module;
using FlowIoC.Editor.Config.ModuleConfig;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Provider
{
    internal static class NamespaceProvider
    {
        private const string MODULES_PATH = "Modules";
        private const string MODULE_INFO_FILE = "_module_info.txt";

        public static void UpdateNamespaceSettings()
        {
            List<string> modulePaths = GetAllModulePaths();
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

            foreach (string modulePath in modulePaths)
            {
                string moduleFolderName = Path.GetFileName(modulePath);

                string[] asmdefFiles = Directory.GetFiles(modulePath, "*.asmdef", SearchOption.TopDirectoryOnly);
                if (asmdefFiles.Length == 0)
                {
                    continue;
                }

                string asmdefFileName = Path.GetFileNameWithoutExtension(asmdefFiles[0]);
                string dotSettingsFileName = asmdefFileName + ".csproj.DotSettings";
                string finalDotSettingsPath = Path.Combine(projectRoot, dotSettingsFileName);

                // Always recreate with correct Rider-native format
                NamespaceUtility.CreateDotSettingsFile(finalDotSettingsPath);

                XmlDocument doc = new XmlDocument();
                doc.Load(finalDotSettingsPath);

                AddNamespaceEntriesForModule_New(doc, modulePath);

                NamespaceUtility.SaveDotSettings(doc, finalDotSettingsPath);
                Debug.Log($"[{moduleFolderName}] => .DotSettings updated: {finalDotSettingsPath}");
            }

            CleanupOrphanedFiles(projectRoot);

            AssetDatabase.Refresh();
            Debug.Log("All module-based .DotSettings files updated successfully.");
        }

        private static void CleanupOrphanedFiles(string projectRoot)
        {
            HashSet<string> existingAssemblyNames = CollectAllAsmdefNames(projectRoot);
            int cleanedCount = 0;

            string[] dotSettingsFiles = Directory.GetFiles(projectRoot, "*.csproj.DotSettings", SearchOption.TopDirectoryOnly);
            foreach (string filePath in dotSettingsFiles)
            {
                if (!filePath.EndsWith(".csproj.DotSettings", StringComparison.OrdinalIgnoreCase)) continue;

                string assemblyName = Path.GetFileName(filePath).Replace(".csproj.DotSettings", "");
                if (!existingAssemblyNames.Contains(assemblyName))
                {
                    File.Delete(filePath);
                    Debug.Log($"[Cleanup] Orphaned DotSettings deleted: {Path.GetFileName(filePath)}");
                    cleanedCount++;
                }
            }

            string[] csprojFiles = Directory.GetFiles(projectRoot, "Modules.*.csproj", SearchOption.TopDirectoryOnly);
            foreach (string filePath in csprojFiles)
            {
                if (!filePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)) continue;

                string assemblyName = Path.GetFileNameWithoutExtension(filePath);
                if (!existingAssemblyNames.Contains(assemblyName))
                {
                    File.Delete(filePath);
                    Debug.Log($"[Cleanup] Orphaned csproj deleted: {Path.GetFileName(filePath)}");
                    cleanedCount++;
                }
            }

            if (cleanedCount > 0)
                Debug.Log($"[Cleanup] Total orphaned files deleted: {cleanedCount}");
        }

        private static HashSet<string> CollectAllAsmdefNames(string projectRoot)
        {
            HashSet<string> names = new HashSet<string>();
            string[] searchPaths =
            {
                Path.Combine(projectRoot, "Assets"),
                Path.Combine(projectRoot, "Packages")
            };

            foreach (string searchPath in searchPaths)
            {
                if (!Directory.Exists(searchPath)) continue;

                string[] asmdefFiles = Directory.GetFiles(searchPath, "*.asmdef", SearchOption.AllDirectories);
                foreach (string asmdefPath in asmdefFiles)
                {
                    names.Add(Path.GetFileNameWithoutExtension(asmdefPath));
                }
            }

            return names;
        }

        private static List<string> GetAllModulePaths()
        {
            List<string> modulePaths = new List<string>();
            string modulesFullPath = Path.Combine(Application.dataPath, MODULES_PATH);

            if (!Directory.Exists(modulesFullPath))
                return modulePaths;

            string[] allDirectories = Directory.GetDirectories(modulesFullPath, "*", SearchOption.AllDirectories);
            foreach (string directory in allDirectories)
            {
                string moduleInfoPath = Path.Combine(directory, MODULE_INFO_FILE);
                if (File.Exists(moduleInfoPath))
                {
                    modulePaths.Add(directory);
                }
            }

            return modulePaths;
        }
        
        private static void AddNamespaceEntriesForModule_New(XmlDocument doc, string modulePath)
        {
            string moduleInfoPath = Path.Combine(modulePath, MODULE_INFO_FILE);
            if (!File.Exists(moduleInfoPath))
                return;
            
            string moduleTypeString = NamespaceUtility.GetModuleTypeFromInfoFile(moduleInfoPath);
            if (!Enum.TryParse(moduleTypeString, out ModuleType moduleType))
            {
                if (moduleTypeString == "Sub")
                    moduleType = ModuleType.Main;
                else
                    moduleType = ModuleType.Main;
            }

            DirectoryStructureConfig config = GetDirectoryStructureConfig(moduleType);
            if (config == null)
            {
                Debug.LogError($"[NEW] Could not find directory structure config for module type {moduleType}");
                return;
            }
            
            List<string> nonNamespaceFolders = new List<string>();
            CollectNonNamespaceFolders(modulePath, config.RootFolders, nonNamespaceFolders);
            
            foreach (string folderPath in nonNamespaceFolders)
            {
                NamespaceUtility.AddNamespaceFolderToSkip(doc, folderPath);
            }

            AddAncestorSkipFolders(modulePath, doc);
        }

        private static DirectoryStructureConfig GetDirectoryStructureConfig(ModuleType moduleType)
        {
            switch (moduleType)
            {
                case ModuleType.Main:
                    return MainModuleDirectoryStructureConfig.GetOrCreateConfig("Main");
                case ModuleType.Screen:
                    return ScreenModuleDirectoryStructureConfig.GetOrCreateConfig("Screen");
                case ModuleType.Test:
                    return TestModuleDirectoryStructureConfig.GetOrCreateConfig("Test");
                default:
                    return null;
            }
        }

        private static void AddAncestorSkipFolders(string modulePath, XmlDocument doc)
        {
            string modulesRoot = Path.Combine(Application.dataPath, MODULES_PATH).Replace('\\', '/');

            DirectoryInfo current = Directory.GetParent(modulePath);
            while (current != null)
            {
                string fullName = current.FullName.Replace('\\', '/');
                if (fullName.Length <= modulesRoot.Length ||
                    !fullName.StartsWith(modulesRoot, StringComparison.OrdinalIgnoreCase))
                    break;

                if (NamespaceUtility.SkipFolderNames.Contains(current.Name, StringComparer.Ordinal))
                {
                    NamespaceUtility.AddNamespaceFolderToSkip(doc, current.FullName);
                }

                current = current.Parent;
            }
        }

        private static void CollectNonNamespaceFolders(string basePath, List<FolderConfig> folders, List<string> nonNamespaceFolders)
        {
            foreach (FolderConfig folder in folders)
            {
                if (!folder.IsMandatory && !folder.IsOptional)
                    continue;

                string folderPath = Path.Combine(basePath, folder.FolderName);

                if (!folder.IsNamespaceProvider)
                {
                    nonNamespaceFolders.Add(folderPath);
                }

                if (folder.SubFolders != null && folder.SubFolders.Count > 0)
                {
                    CollectNonNamespaceFolders(folderPath, folder.SubFolders, nonNamespaceFolders);
                }
            }
        }
    }
}
#endif
