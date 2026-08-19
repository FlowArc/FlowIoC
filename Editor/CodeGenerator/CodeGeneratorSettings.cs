#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlowIoC.Editor.CodeGenerator.Extensions;
using FlowIoC.Editor.Config.ModuleConfig;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator
{
    [CreateAssetMenu(menuName = "FlowIoC/Editor/CodeGenerator/CodeGeneratorSettings", fileName = "CodeGeneratorSettings", order = 1)]
    public class CodeGeneratorSettings : ScriptableObject
    {
        public List<AssemblyDefinitionAsset> AssemblyDefinitions;
        
        [HideInInspector] [SerializeField] 
        public SerializableDictionary<FolderConfig.FolderType, string> DirectoryStructureConfigMap = 
            new SerializableDictionary<FolderConfig.FolderType, string>
        {
            {FolderConfig.FolderType.SubModules, "zSubModules"},
            {FolderConfig.FolderType.TestModules, "zTestModules"},
            {FolderConfig.FolderType.ScreenModules, "zScreenModules"},
            {FolderConfig.FolderType.ViewsAndMediators, "ViewsMediators"},
            {FolderConfig.FolderType.ScreenConfigs, "ScreenConfigs"},
            {FolderConfig.FolderType.ScreenViews, "ScreenViews"},
            {FolderConfig.FolderType.RootsAndContexts, "RootsContexts"},
            {FolderConfig.FolderType.Services, "Services"},
            {FolderConfig.FolderType.Controllers, "Controllers"},
            {FolderConfig.FolderType.Models, "Models"},
            {FolderConfig.FolderType.UnityObjects, "UnityObjects"},
            {FolderConfig.FolderType.ValueObjects, "ValueObjects"},
            {FolderConfig.FolderType.Editor, "Editor"},
            {FolderConfig.FolderType.Resources, "Resources"},
            {FolderConfig.FolderType.Prefabs, "Prefabs"},
            {FolderConfig.FolderType.Scenes, "Scenes"}
        };

        [HideInInspector] [SerializeField] 
        public SerializableDictionary<string, string> DirectoryStructureConfigPaths = 
            new SerializableDictionary<string, string>
        {
            {"Main", "Assets/Editor/FlowIoC/CodeGenerator/MainModuleDirectoryStructureConfig.asset"},
            {"Screen", "Assets/Editor/FlowIoC/CodeGenerator/ScreenModuleDirectoryStructureConfig.asset"},
            {"Test", "Assets/Editor/FlowIoC/CodeGenerator/TestModuleDirectoryStructureConfig.asset"}
        };

        public static void CreateConfig()
        {
            string fullPath = Path.GetDirectoryName(CodeGeneratorStrings.CONFIG_PATH);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }

            CodeGeneratorSettings settings = AssetDatabase.LoadAssetAtPath<CodeGeneratorSettings>(CodeGeneratorStrings.CONFIG_PATH);
            if (settings != null) return;
            settings = CreateInstance<CodeGeneratorSettings>();
            AssetDatabase.CreateAsset(settings, CodeGeneratorStrings.CONFIG_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"CodeGeneratorSettings asset created at: {CodeGeneratorStrings.CONFIG_PATH}");
        }
        
        public void UpdateLockedFolderInfoFiles()
        {
            string modulesPath = Path.Combine(Application.dataPath, "Modules");
            if (!Directory.Exists(modulesPath)) return;

            var folderOperations = new List<(string oldPath, string newPath, FolderConfig.FolderType type)>();
            CollectFolderOperations(modulesPath, folderOperations);
            AssetDatabase.Refresh();

            foreach (var operation in folderOperations)
            {
                try
                {
                    if (Directory.Exists(operation.oldPath))
                    {
                        string assetOldPath = ConvertAbsolutePathToAssetPath(operation.oldPath);
                        string newName = Path.GetFileName(operation.newPath);

                        if (!string.Equals(Path.GetFileName(operation.oldPath), newName, StringComparison.InvariantCultureIgnoreCase))
                        {
                            string renameResult = AssetDatabase.RenameAsset(assetOldPath, newName);
                            if (!string.IsNullOrEmpty(renameResult))
                            {
                                continue;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Exception handling if necessary
                }
            }

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private string ConvertAbsolutePathToAssetPath(string absolutePath)
        {
            absolutePath = absolutePath.Replace("\\", "/");
            return "Assets" + absolutePath.Substring(Application.dataPath.Length);
        }

        private void CollectFolderOperations(
            string currentPath, 
            List<(string oldPath, string newPath, FolderConfig.FolderType type)> operations
        )
        {
            if (!Directory.Exists(currentPath)) return;

            foreach (KeyValuePair<FolderConfig.FolderType, string> kvp in DirectoryStructureConfigMap)
            {
                string[] possibleFolders = Directory.GetDirectories(currentPath);
                string oldFolderPath = possibleFolders.FirstOrDefault(f =>
                {
                    string infoFilePath = Path.Combine(f, $"_{kvp.Key.ToString().ToLower()}_info.txt");
                    return File.Exists(infoFilePath);
                });

                if (!string.IsNullOrEmpty(oldFolderPath))
                {
                    string newFolderPath = Path.Combine(Path.GetDirectoryName(oldFolderPath), kvp.Value);
                    if (!string.Equals(oldFolderPath, newFolderPath, StringComparison.InvariantCultureIgnoreCase))
                    {
                        operations.Add((oldFolderPath, newFolderPath, kvp.Key));
                    }

                    string[] subDirs = Directory.GetDirectories(oldFolderPath);
                    foreach (string subDir in subDirs)
                    {
                        CollectFolderOperations(subDir, operations);
                    }
                }
            }

            string[] remainingSubDirs = Directory.GetDirectories(currentPath);
            foreach (string subDir in remainingSubDirs)
            {
                bool isProcessed = false;
                foreach (KeyValuePair<FolderConfig.FolderType, string> kvp in DirectoryStructureConfigMap)
                {
                    string infoFilePath = Path.Combine(subDir, $"_{kvp.Key.ToString().ToLower()}_info.txt");
                    if (File.Exists(infoFilePath))
                    {
                        isProcessed = true;
                        break;
                    }
                }

                if (!isProcessed)
                {
                    CollectFolderOperations(subDir, operations);
                }
            }
        }
    }
}
#endif
