#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.Config.ModuleConfig;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration
{
    internal partial class ModuleGenerator
    {
        private static void HandleStandardModuleCreation(
            string moduleName,
            string modulePath,
            ModuleType selectedModuleType,
            Dictionary<ModuleType, DirectoryStructureConfig> directoryConfigMap,
            bool createRoot,
            bool createContext,
            bool createScreen,
            bool makeRootSingleton
        )
        {
            string rootsAndContextsPath = directoryConfigMap[selectedModuleType]
                .FindFullFolderPathByID(FolderConfig.FolderType.RootsAndContexts, modulePath);

            if (!string.IsNullOrEmpty(rootsAndContextsPath))
            {
                if (createRoot)
                {
                    CreateRoot(rootsAndContextsPath, modulePath, moduleName, selectedModuleType == ModuleType.Test, makeRootSingleton);
                }

                if (createContext)
                {
                    CreateContext(rootsAndContextsPath, modulePath, moduleName, selectedModuleType == ModuleType.Test);
                }
            }
            else
            {
                Debug.LogWarning(ROOTS_CONTEXTS_WARNING);
            }

            if (createScreen)
            {
                string scenePath = directoryConfigMap[selectedModuleType]
                    .FindFullFolderPathByID(FolderConfig.FolderType.Scenes, modulePath);
                CreateScene(scenePath, moduleName);
                EditorPrefs.SetBool(BOOL_CREATE_SCREEN, true);
            }
        }

        private static void CreateRoot(string path, string modulePath, string moduleName, bool isTest, bool makeSingleton)
        {
            string suffix = isTest ? "" : "";
            string rootName = moduleName + suffix + "Root";
            string contextName = moduleName + suffix + "Context";

            string moduleNamespace = NamespaceUtility.GetModuleNamespace(modulePath);
            string rootsAndContextsNamespace = $"{moduleNamespace}.RootsContexts";

            string tempRootName = makeSingleton ? "TempSingletonRoot" : "TempRoot";
            string tempRootPath = makeSingleton ? CodeGeneratorStrings.TempSingletonRootPath : CodeGeneratorStrings.TempRootPath;

            CodeGeneratorUtils.CreateRoot(
                rootName,
                contextName,
                "TempContext",
                tempRootName,
                path,
                tempRootPath,
                rootsAndContextsNamespace,
                isTest
            );
        }

        private static void CreateContext(string path, string modulePath, string moduleName, bool isTest)
        {
            string suffix = isTest ? "" : "";
            string rootName = moduleName + suffix + "Root";
            string contextName = moduleName + suffix + "Context";

            string moduleNamespace = NamespaceUtility.GetModuleNamespace(modulePath);
            string rootsAndContextsNamespace = $"{moduleNamespace}.RootsContexts";

            CodeGeneratorUtils.CreateContext(
                contextName,
                "TempContext",
                path,
                CodeGeneratorStrings.TempContextPath,
                rootsAndContextsNamespace,
                false,
                isTest
            );
            EditorPrefs.SetString(KEY_CONTEXT_NAMESPACE, rootsAndContextsNamespace);
        }

        private static void CreateFoldersRecursively(string basePath, List<FolderConfig> folders, List<FolderConfig> selectedOptionalFolders)
        {
            foreach (FolderConfig folder in folders)
            {
                if (!folder.IsMandatory && !folder.IsOptional) continue;
                if (folder.IsMandatory || (folder.IsOptional && selectedOptionalFolders.Contains(folder)))
                {
                    string folderPath = Path.Combine(basePath, folder.FolderName);
                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                        AssetDatabase.ImportAsset(
                            NamespaceUtility.GetUnityAssetPath(folderPath),
                            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ImportRecursive
                        );
                    }

                    if (folder.SubFolders != null && folder.SubFolders.Count > 0)
                    {
                        CreateFoldersRecursively(folderPath, folder.SubFolders, selectedOptionalFolders);
                    }
                }
            }
        }

        private static void CreateInfoFiles(
            string baseModulePath,
            ModuleType moduleType,
            List<FolderConfig> selectedOptionalFolders,
            Dictionary<ModuleType, DirectoryStructureConfig> directoryConfigMap
        )
        {
            DirectoryStructureConfig moduleConfig = directoryConfigMap[moduleType];
            foreach (FolderConfig rootFolder in moduleConfig.RootFolders)
            {
                if (!rootFolder.IsMandatory && !rootFolder.IsOptional) continue;
                if (rootFolder.IsMandatory || (rootFolder.IsOptional && selectedOptionalFolders.Contains(rootFolder)))
                {
                    if (rootFolder.Type == FolderConfig.FolderType.SubModules ||
                        rootFolder.Type == FolderConfig.FolderType.TestModules ||
                        rootFolder.Type == FolderConfig.FolderType.ScreenModules)
                    {
                        string folderPath = Path.Combine(baseModulePath, rootFolder.FolderName);
                        if (Directory.Exists(folderPath))
                        {
                            string infoFileName = rootFolder.Type switch
                            {
                                FolderConfig.FolderType.SubModules => SUBMODULES_INFO_FILE,
                                FolderConfig.FolderType.TestModules => TESTMODULES_INFO_FILE,
                                FolderConfig.FolderType.ScreenModules => SCREENMODULES_INFO_FILE,
                                _ => null
                            };

                            if (infoFileName != null)
                            {
                                string filePath = Path.Combine(folderPath, infoFileName);
                                if (!File.Exists(filePath))
                                {
                                    File.WriteAllText(filePath, "Modules:\n");
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void UpdateParentModuleInfo(string baseModulePath, string modulePath, string moduleType)
        {
            string parentInfoFileName = moduleType switch
            {
                "Sub" => SUBMODULES_INFO_FILE,
                "Test" => TESTMODULES_INFO_FILE,
                "Screen" => SCREENMODULES_INFO_FILE,
                "Main" => MAIN_MODULES_INFO_FILE,
                _ => null
            };

            if (parentInfoFileName != null)
            {
                string parentInfoFilePath = Path.Combine(baseModulePath, parentInfoFileName);
                if (!File.Exists(parentInfoFilePath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(parentInfoFilePath));
                    File.WriteAllText(parentInfoFilePath, "Modules:\n");
                }

                File.AppendAllText(parentInfoFilePath, Path.GetFileName(modulePath) + "\n");
            }
        }

        private static void ProcessLockedFoldersRecursively(string currentPath, CodeGeneratorSettings codeGenSettings)
        {
            foreach (var kvp in codeGenSettings.DirectoryStructureConfigMap)
            {
                string folderPath = Path.Combine(currentPath, kvp.Value);
                if (Directory.Exists(folderPath))
                {
                    string infoFilePath = Path.Combine(folderPath, $"_{kvp.Key.ToString().ToLower()}_info.txt");
                    File.WriteAllText(infoFilePath, $"Name: {kvp.Value}");
                }
            }

            foreach (string subDir in Directory.GetDirectories(currentPath))
            {
                ProcessLockedFoldersRecursively(subDir, codeGenSettings);
            }
        }
    }
}
#endif