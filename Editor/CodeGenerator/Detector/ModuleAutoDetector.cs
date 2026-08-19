#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using FlowIoC.ConsoleModule;
using FlowIoC.Editor.Config.ModuleConfig;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Detector
{
    internal static class ModuleAutoDetector
    {
        private const string InitializedKey = "ModuleAutoDetector_Initialized";
        private static readonly List<string> DetectedModulesWithoutTool = new();
        private static readonly List<(string Name, int Value, Color LogColor)> PendingLogTypes = new();
        private static CodeGeneratorSettings _codeGenSettings;

        [InitializeOnLoadMethod]
        private static void OnProjectLoad()
        {
            if (!SessionState.GetBool(InitializedKey, false))
            {
                SessionState.SetBool(InitializedKey, true);
                EditorApplication.delayCall += DetectAndRegisterModulesOnStartup;
            }
        }

        public static void DetectAndRegisterModulesOnStartup()
        {
            DetectAndRegisterModules();
            if (DetectedModulesWithoutTool.Count > 0)
            {
                ShowToolReminderPopup();
            }
        }

        public static void RescanModules()
        {
            DetectAndRegisterModules();
        }

        private static void DetectAndRegisterModules()
        {
            PendingLogTypes.Clear();

            string assetsModulesPath = Path.Combine(Application.dataPath, "Modules");
            if (Directory.Exists(assetsModulesPath))
                ProcessModulesRecursively(assetsModulesPath);

            ScanPackagesForModules();

            if (PendingLogTypes.Count > 0)
            {
                FlowLogTypeManager.AddFlowLogTypesBatch(PendingLogTypes);
                PendingLogTypes.Clear();
            }

            AssetDatabase.Refresh();
        }

        private static void ScanPackagesForModules()
        {
            string packagesPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Packages"));
            if (!Directory.Exists(packagesPath)) return;

            foreach (string packageDir in Directory.GetDirectories(packagesPath, "*", SearchOption.TopDirectoryOnly))
            {
                string[] modulesDirs = Directory.GetDirectories(packageDir, "Modules", SearchOption.AllDirectories);
                foreach (string modulesDir in modulesDirs)
                {
                    ProcessModulesRecursively(modulesDir);
                }
            }
        }

        private static void ProcessModulesRecursively(string directory)
        {
            string[] allSubDirectories = Directory.GetDirectories(directory, "*", SearchOption.TopDirectoryOnly);

            foreach (string subDir in allSubDirectories)
            {
                string subDirName = Path.GetFileName(subDir);

                if (subDirName.EndsWith("Module", StringComparison.OrdinalIgnoreCase))
                {
                    string moduleName = subDirName;
                    ProcessModule(subDir, moduleName);
                }

                ProcessModulesRecursively(subDir);
            }
        }

        private static void ProcessModule(string directory, string moduleName)
        {
            string moduleInfoPath = Path.Combine(directory, "_module_info.txt");
            string moduleType = DetermineModuleType(directory);

            DirectoryInfo parentDirectoryInfo = Directory.GetParent(directory);
            string parentModulePath = parentDirectoryInfo != null ? parentDirectoryInfo.FullName : null;

            HandleModule(moduleName, moduleInfoPath, moduleType, parentModulePath);
        }

        private static string DetermineModuleType(string modulePath)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(modulePath);
            DirectoryInfo parentDirectory = directoryInfo.Parent;

            if (parentDirectory != null)
            {
                string parentFolderName = parentDirectory.Name;


                if (!LoadCodeGeneratorSettings())
                {
                    CodeGeneratorSettings.CreateConfig();
                }

                if (parentFolderName.Equals(_codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.SubModules], StringComparison.OrdinalIgnoreCase))
                {
                    return "Sub";
                }
                else if (parentFolderName.Equals(_codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.TestModules], StringComparison.OrdinalIgnoreCase))
                {
                    return "Test";
                }
                else if (parentFolderName.Equals(_codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.ScreenModules], StringComparison.OrdinalIgnoreCase))
                {
                    return "Screen";
                }
                else if (parentFolderName.Equals("Modules", StringComparison.OrdinalIgnoreCase))
                {
                    return "Main";
                }
            }

            return "Main";
        }

        private static bool LoadCodeGeneratorSettings()
        {
            _codeGenSettings = AssetDatabase.LoadAssetAtPath<CodeGeneratorSettings>(CodeGeneratorStrings.CONFIG_PATH);
            if (_codeGenSettings == null)
            {
                Debug.LogError($"CodeGeneratorSettings asset not found. Please ensure it exists at {CodeGeneratorStrings.CONFIG_PATH}.");
                return true;
            }

            return false;
        }

        private static string GetRelatedInfoFileName(string moduleType)
        {
            return moduleType switch
            {
                "Sub" => "_submodules_info.txt",
                "Test" => "_testmodules_info.txt",
                "Screen" => "_screenmodules_info.txt",
                "Main" => "mainmodules_info.txt",
                _ => null
            };
        }

        private static void HandleModule(string moduleName, string moduleInfoPath, string moduleType, string parentModulePath)
        {
            bool needToAddToToolList = false;

            if (!File.Exists(moduleInfoPath))
            {
                File.WriteAllText(moduleInfoPath, $"ModuleName: {moduleName}\nModuleType: {moduleType}\n");
                needToAddToToolList = true;
            }
            else
            {
                string[] lines = File.ReadAllLines(moduleInfoPath);
                string existingModuleName = GetKeyValueFromLines(lines, "ModuleName");
                string existingModuleType = GetKeyValueFromLines(lines, "ModuleType");

                if (!string.Equals(existingModuleName, moduleName, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(existingModuleType, moduleType, StringComparison.OrdinalIgnoreCase))
                {
                    File.WriteAllText(moduleInfoPath, $"ModuleName: {moduleName}\nModuleType: {moduleType}\n");
                    needToAddToToolList = true;
                }
            }

            if (needToAddToToolList)
            {
                DetectedModulesWithoutTool.Add(moduleName);
            }

            if (moduleType != "Test")
            {
                QueueLogTypeRegistration(moduleName);
            }

            if (parentModulePath != null)
            {
                string relatedInfoFileName = GetRelatedInfoFileName(moduleType);
                if (relatedInfoFileName != null)
                {
                    string relatedInfoPath = Path.Combine(parentModulePath, relatedInfoFileName);
                    if (!File.Exists(relatedInfoPath))
                    {
                        File.WriteAllText(relatedInfoPath, "Modules:\n");
                    }

                    AppendModuleToFile(relatedInfoPath, moduleName);
                }
            }
        }

        private static string GetKeyValueFromLines(string[] lines, string key)
        {
            foreach (var line in lines)
            {
                if (line.StartsWith(key, StringComparison.OrdinalIgnoreCase))
                {
                    return line.Split(new[] {':'}, 2)[1].Trim();
                }
            }

            return null;
        }

        private static void AppendModuleToFile(string filePath, string moduleName)
        {
            string[] existingEntries = File.ReadAllLines(filePath);
            bool exists = false;

            foreach (string entry in existingEntries)
            {
                if (entry.Trim().Equals(moduleName, StringComparison.OrdinalIgnoreCase))
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                File.AppendAllText(filePath, moduleName + "\n");
            }
        }

        private static void QueueLogTypeRegistration(string moduleName)
        {
            foreach (var pending in PendingLogTypes)
            {
                if (string.Equals(pending.Name, moduleName, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            PendingLogTypes.Add((moduleName, -1, Color.white));
        }

        private static void ShowToolReminderPopup()
        {
            if (DetectedModulesWithoutTool.Count > 0)
            {
                string moduleList = string.Join("\n", DetectedModulesWithoutTool);
                bool showPopup = EditorUtility.DisplayDialog(
                    "Module Creation Reminder",
                    "The following modules were detected without using the editor tool:\n\n" +
                    moduleList +
                    "\n\nIt is recommended to use the editor tool for creating modules to ensure correct structure.",
                    "OK"
                );

                if (showPopup)
                {
                    DetectedModulesWithoutTool.Clear();
                    AssetDatabase.Refresh();
                }
            }
        }
    }
}
#endif