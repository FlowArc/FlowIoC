#if UNITY_EDITOR
using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using FlowIoC.Editor.CodeGenerator.Menus.Module;
using FlowIoC.Editor.Config.ModuleConfig;
using FlowIoC.Editor.Modules;

namespace FlowIoC.Editor.CodeGenerator.Menus
{
    public static class CreateAssemblyFromContextMenu
    {
        private const string ASMDEF_EXT = ".asmdef";

        [MenuItem("Assets/FlowIoC/Create Assembly", true)]
        private static bool ValidateCreateAssembly()
        {
            if (Selection.activeObject == null) return false;

            string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);

            if (!AssetDatabase.IsValidFolder(assetPath))
            {
                var parentFolder = Path.GetDirectoryName(assetPath);
                if (string.IsNullOrEmpty(parentFolder) || !AssetDatabase.IsValidFolder(parentFolder))
                    return false;

                assetPath = parentFolder;
            }

            ModuleRegistry registry = new ModuleRegistryFactory().FromProject();
            return registry.IsModule(assetPath);
        }

        [MenuItem("Assets/FlowIoC/Create Assembly", priority = 20)]
        private static void CreateAssembly()
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

            string fullPath = new ModuleAssetPathResolver().ToAbsolutePath(assetPath);
            string moduleName = module.Name;

            string asmdefPath = Path.Combine(fullPath, moduleName + ASMDEF_EXT);
            if (!File.Exists(asmdefPath))
            {
                CreateAssemblyDefinitionFile(asmdefPath, moduleName);
            }
            else
            {
                Debug.LogWarning($".asmdef file already exists at: {asmdefPath}");
            }

            CreateDotSettingsFileInNewFormat(fullPath, moduleName);

            DirectoryStructureConfig config = new DirectoryStructureConfigProvider().ConfigFor(module.Kind);
            if (config == null)
            {
                Debug.LogError($"No directory structure config found for '{module.Kind}'. Skipping provider setup.");
            }
            else
            {
                var finalAssemblyName = GetParsedAssemblyName(moduleName);
                string newDotSettingsPath = Path.Combine(fullPath, finalAssemblyName + ".csproj.DotSettings");

                TraverseFoldersAndSetProviders(fullPath, config.RootFolders, newDotSettingsPath);
                NamespaceUtility.SetNamespaceProvider(fullPath, true, newDotSettingsPath);
            }

            UpdateAllScriptsNamespaceInFolder(fullPath);

            AssetDatabase.Refresh();
            Debug.Log($"Assembly & namespace update done in: {assetPath}");
        }

        private static void TraverseFoldersAndSetProviders(string basePath,
            System.Collections.Generic.List<FolderConfig> folders, string dotSettingsPath)
        {
            foreach (var folder in folders)
            {
                string folderPath = Path.Combine(basePath, folder.FolderName);
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

        private static void UpdateAllScriptsNamespaceInFolder(string folderPath)
        {
            var csFiles = Directory.GetFiles(folderPath, "*.cs", SearchOption.AllDirectories);
            Regex regex = new Regex(@"(^|\r?\n)\s*namespace\s+([A-Za-z0-9_.]+)");

            foreach (var file in csFiles)
            {
                if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                    continue;

                string fileDirectory = Path.GetDirectoryName(file);
                if (!NamespaceUtility.IsFolderNamespaceProvider(fileDirectory))
                    continue;

                string finalNamespace = NamespaceUtility.GetFullNamespaceForFile(file);

                string content = File.ReadAllText(file);
                if (regex.IsMatch(content))
                {
                    string modified = regex.Replace(content, $"$1namespace {finalNamespace}");
                    if (modified != content)
                    {
                        File.WriteAllText(file, modified);
                        Debug.Log($"Updated namespace in: {file} => {finalNamespace}");
                    }
                }
            }
        }

        private static string GetParsedAssemblyName(string rawAssemblyName) =>
            new ModuleAssemblyName().From(rawAssemblyName);

        private static void CreateAssemblyDefinitionFile(string oldFilePath, string rawAssemblyName)
        {
            var finalAssemblyName = GetParsedAssemblyName(rawAssemblyName);

            var asmdefContent = $@"{{
  ""name"": ""{finalAssemblyName}"",
  ""references"": [
    ""FlowIoC""
  ],
  ""includePlatforms"": [],
  ""excludePlatforms"": [],
  ""allowUnsafeCode"": false,
  ""overrideReferences"": false,
  ""precompiledReferences"": [],
  ""autoReferenced"": true,
  ""defineConstraints"": [],
  ""versionDefines"": [],
  ""noEngineReferences"": false
}}";

            string directory = Path.GetDirectoryName(oldFilePath) ?? "";
            string newFileName = finalAssemblyName + ".asmdef";
            string newFilePath = Path.Combine(directory, newFileName);

            File.WriteAllText(newFilePath, asmdefContent);
            Debug.LogError($"Assembly Definition created at: {newFilePath} (Name: {finalAssemblyName}, references: FlowIoC)");

            if (!oldFilePath.Equals(newFilePath, StringComparison.OrdinalIgnoreCase) && File.Exists(oldFilePath))
            {
                File.Delete(oldFilePath);
                string oldAssetPath = oldFilePath;
                AssetDatabase.DeleteAsset(oldAssetPath);
                Debug.Log($"Deleted old Assembly Definition file: {oldFilePath}");
            }

            AssetDatabase.Refresh();
        }

        private static void CreateDotSettingsFileInNewFormat(string fullPath, string rawAssemblyName)
        {
            string oldDotSettingsPath = Path.Combine(fullPath, rawAssemblyName + ".csproj.DotSettings");

            var finalAssemblyName = GetParsedAssemblyName(rawAssemblyName);
            string newDotSettingsPath = Path.Combine(fullPath, finalAssemblyName + ".csproj.DotSettings");

            if (!File.Exists(newDotSettingsPath))
            {
                NamespaceUtility.CreateDotSettingsFile(newDotSettingsPath);
                Debug.Log($".DotSettings file created => {newDotSettingsPath}");
            }
            else
            {
                Debug.LogWarning($"DotSettings file already exists: {newDotSettingsPath}");
            }

            if (!oldDotSettingsPath.Equals(newDotSettingsPath, StringComparison.OrdinalIgnoreCase) &&
                File.Exists(oldDotSettingsPath))
            {
                File.Delete(oldDotSettingsPath);
                Debug.Log($"Deleted old .DotSettings file: {oldDotSettingsPath}");
            }
        }
    }
}
#endif