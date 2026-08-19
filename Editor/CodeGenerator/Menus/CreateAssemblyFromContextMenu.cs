#if UNITY_EDITOR
using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using FlowIoC.Editor.CodeGenerator.Menus.Module;
using FlowIoC.Editor.Config.ModuleConfig;

namespace FlowIoC.Editor.CodeGenerator.Menus
{
    public static class CreateAssemblyFromContextMenu
    {
        private const string MODULE_INFO_FILE = "_module_info.txt";
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

            string fullPath = Path.Combine(Application.dataPath, assetPath.Replace("Assets/", ""));
            string moduleInfoPath = Path.Combine(fullPath, MODULE_INFO_FILE);
            return File.Exists(moduleInfoPath);
        }

        [MenuItem("Assets/FlowIoC/Create Assembly", priority = 20)]
        private static void CreateAssembly()
        {
            string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (!AssetDatabase.IsValidFolder(assetPath))
            {
                assetPath = Path.GetDirectoryName(assetPath);
            }
            string fullPath = Path.Combine(Application.dataPath, assetPath.Replace("Assets/", ""));
            
            string moduleName = GetModuleName(fullPath);
            if (string.IsNullOrEmpty(moduleName))
            {
                Debug.LogError($"Module name not found in {MODULE_INFO_FILE}. Aborting.");
                return;
            }
            
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
            
            string moduleInfoFile = Path.Combine(fullPath, MODULE_INFO_FILE);
            string moduleTypeString = NamespaceUtility.GetModuleTypeFromInfoFile(moduleInfoFile);
            if (!Enum.TryParse(moduleTypeString, out ModuleType modType))
            {
                modType = ModuleType.Main;
            }

            DirectoryStructureConfig config = GetDirectoryStructureConfigFor(modType);
            if (config == null)
            {
                Debug.LogError($"No directory structure config found for '{modType}'. Skipping provider setup.");
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

        private static DirectoryStructureConfig GetDirectoryStructureConfigFor(ModuleType moduleType)
        {
            switch (moduleType)
            {
                case ModuleType.Main:
                    return MainModuleDirectoryStructureConfig.GetOrCreateConfig("Main");
                case ModuleType.Screen:
                    return ScreenModuleDirectoryStructureConfig.GetOrCreateConfig("Screen");
                case ModuleType.Test:
                    return TestModuleDirectoryStructureConfig.GetOrCreateConfig("Test");
            }
            return null;
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
        
        private static string GetModuleName(string folderPath)
        {
            string moduleInfoPath = Path.Combine(folderPath, MODULE_INFO_FILE);
            if (!File.Exists(moduleInfoPath))
            {
                Debug.LogError($"[GetModuleName] File not found => {moduleInfoPath}");
                return null;
            }

            string[] lines = File.ReadAllLines(moduleInfoPath);
            foreach (var rawLine in lines)
            {
                string line = rawLine.TrimStart();
                if (line.StartsWith("ModuleName: "))
                {
                    string val = line.Substring("ModuleName: ".Length).Trim();
                    return val;
                }
            }
            return null;
        }
        
        private static string GetParsedAssemblyName(string rawAssemblyName)
        {
            const string prefix = "Modules.";
            string moduleName = rawAssemblyName;
            string suffix = string.Empty;

            if (rawAssemblyName.EndsWith("ScreenTestModule", StringComparison.OrdinalIgnoreCase))
            {
                suffix = ".Screen.Test";
                moduleName = rawAssemblyName.Substring(0, rawAssemblyName.Length - 12);
            }
            else if (rawAssemblyName.EndsWith("ScreenModule", StringComparison.OrdinalIgnoreCase))
            {
                suffix = ".Screen";
                moduleName = rawAssemblyName.Substring(0, rawAssemblyName.Length - 12);
            }
            else if (rawAssemblyName.EndsWith("TestModule", StringComparison.OrdinalIgnoreCase))
            {
                suffix = ".Test";
                moduleName = rawAssemblyName.Substring(0, rawAssemblyName.Length - 10);
            }
            else if (rawAssemblyName.EndsWith("Module", StringComparison.OrdinalIgnoreCase))
            {
                moduleName = rawAssemblyName.Substring(0, rawAssemblyName.Length - 6);
            }

            moduleName = moduleName.Trim();

            return prefix + moduleName + suffix;
        }
        
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
