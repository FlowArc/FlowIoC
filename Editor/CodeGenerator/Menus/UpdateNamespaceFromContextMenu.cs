#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using FlowIoC.Editor.CodeGenerator.Menus.Module;
using FlowIoC.Editor.Config.ModuleConfig;
using FlowIoC.Editor.Modules;

namespace FlowIoC.Editor.CodeGenerator.Menus
{
    public static class UpdateNamespaceFromContextMenu
    {
        [MenuItem("Assets/FlowIoC/Update Module's Namespaces", true)]
        private static bool ValidateUpdateNamespace()
        {
            if (Selection.activeObject == null)
                return false;

            string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (!AssetDatabase.IsValidFolder(assetPath))
            {
                var parentFolder = Path.GetDirectoryName(assetPath);
                if (string.IsNullOrEmpty(parentFolder) || !AssetDatabase.IsValidFolder(parentFolder))
                    return false;

                assetPath = parentFolder;
            }

            ModuleRegistry registry = new ModuleRegistryFactory().FromProject();
            return registry.TryGetModule(assetPath, out _);
        }

        [MenuItem("Assets/FlowIoC/Update Module's Namespaces", priority = 20)]
        private static void UpdateNamespace()
        {
            string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (!AssetDatabase.IsValidFolder(assetPath))
            {
                assetPath = Path.GetDirectoryName(assetPath);
            }

            ModuleRegistry registry = new ModuleRegistryFactory().FromProject();
            if (!registry.TryGetModule(assetPath, out ModuleDescriptor module))
            {
                Debug.LogError($"'{assetPath}' is not a module.");
                return;
            }

            string moduleFullPath = Path.Combine(Application.dataPath, assetPath.Replace("Assets/", ""));

            string[] asmdefFiles = Directory.GetFiles(moduleFullPath, "*.asmdef", SearchOption.TopDirectoryOnly);
            if (asmdefFiles.Length == 0)
            {
                Debug.LogWarning($"No .asmdef file found in module folder: {moduleFullPath}");
                return;
            }

            string asmdefPath = asmdefFiles[0];
            string asmdefName = Path.GetFileNameWithoutExtension(asmdefPath);
            string dotSettingsName = asmdefName + ".csproj.DotSettings";
            string dotSettingsPath = Path.Combine(moduleFullPath, dotSettingsName);

            if (!File.Exists(dotSettingsPath))
            {
                NamespaceUtility.CreateDotSettingsFile(dotSettingsPath);
                Debug.Log($".DotSettings file created: {dotSettingsPath}");
            }

            DirectoryStructureConfig config = new DirectoryStructureConfigProvider().ConfigFor(module.Kind);
            if (config == null)
            {
                Debug.LogError($"No DirectoryStructureConfig found for ModuleKind: {module.Kind}");
            }
            else
            {
                TraverseFoldersAndSetProviders(moduleFullPath, config.RootFolders, dotSettingsPath);
            }

            MarkAncestorSkipFolders(moduleFullPath, dotSettingsPath);

            NamespaceUtility.SetNamespaceProvider(moduleFullPath, true, dotSettingsPath);

            UpdateAllScriptsNamespaceInFolder(moduleFullPath);

            AssetDatabase.Refresh();
            Debug.Log($"Namespace update is complete. Updated folder: {assetPath}");
        }

        private static void TraverseFoldersAndSetProviders(string baseModulePath,
            List<FolderConfig> folders, string dotSettingsPath)
        {
            foreach (FolderConfig folder in folders)
            {
                string folderPath = Path.Combine(baseModulePath, folder.FolderName);
                if (Directory.Exists(folderPath))
                {
                    bool isProvider = folder.IsNamespaceProvider;

                    NamespaceUtility.SetNamespaceProvider(folderPath, isProvider, dotSettingsPath);

                    if (folder.SubFolders != null && folder.SubFolders.Count > 0)
                    {
                        TraverseFoldersAndSetProviders(folderPath, folder.SubFolders, dotSettingsPath);
                    }
                }
            }
        }

        private static void MarkAncestorSkipFolders(string modulePath, string dotSettingsPath)
        {
            string modulesRoot = Path.Combine(Application.dataPath, "Modules").Replace('\\', '/');

            DirectoryInfo current = Directory.GetParent(modulePath);
            while (current != null)
            {
                string fullName = current.FullName.Replace('\\', '/');
                if (fullName.Length <= modulesRoot.Length ||
                    !fullName.StartsWith(modulesRoot, StringComparison.OrdinalIgnoreCase))
                    break;

                if (NamespaceUtility.SkipFolderNames.Contains(current.Name, StringComparer.Ordinal))
                {
                    NamespaceUtility.SetNamespaceProvider(current.FullName, false, dotSettingsPath);
                }

                current = current.Parent;
            }
        }

        private static void UpdateAllScriptsNamespaceInFolder(string folderPath)
        {
            var csFiles = Directory.GetFiles(folderPath, "*.cs", SearchOption.AllDirectories);
            Regex regex = new Regex(@"(^|\r?\n)\s*namespace\s+([A-Za-z0-9_.]+)");

            foreach (var file in csFiles)
            {
                if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                    continue;

                string fileDir = Path.GetDirectoryName(file);
                if (!NamespaceUtility.IsFolderNamespaceProvider(fileDir))
                {
                    Debug.Log($"Skipping: {file} => provider=false");
                    continue;
                }

                string finalNamespace = NamespaceUtility.GetFullNamespaceForFile(file);

                string content = File.ReadAllText(file);
                if (regex.IsMatch(content))
                {
                    string modified = regex.Replace(content, $"$1namespace {finalNamespace}");
                    if (modified != content)
                    {
                        File.WriteAllText(file, modified);
                    }
                }
            }
        }
    }
}
#endif