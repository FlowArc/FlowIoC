#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.CodeGenerator;
using FlowIoC.Editor.Migration;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.Config.ModuleConfig
{
    [CreateAssetMenu(fileName = "ED_TestModuleDirectoryStructure",
        menuName = "FlowIoC/Editor/CodeGenerator/TestModule Directory Structure Config")]
    public class ED_TestModuleDirectoryStructure : DirectoryStructureConfig
    {
        [field: SerializeReference]
        protected internal override List<FolderEVO> RootFolders { get; protected set; } = new List<FolderEVO>
        {
            new FolderEVO
            {
                FolderName = "Art", Type = FolderEVO.FolderType.Folder, IsMandatory = false, IsOptional = true, IsNamespaceProvider = true
            },
            new FolderEVO
            {
                FolderName = "Prefabs", Type = FolderEVO.FolderType.Prefabs, IsMandatory = true, IsNamespaceProvider = true
            },
            new FolderEVO
            {
                FolderName = "Resources", Type = FolderEVO.FolderType.Resources, IsMandatory = false, IsOptional = true, IsNamespaceProvider = true
            },
            new FolderEVO
            {
                FolderName = "Scenes", Type = FolderEVO.FolderType.Scenes, IsMandatory = false, IsOptional = true, IsNamespaceProvider = true
            },
            new FolderEVO()
            {
                FolderName = "Scriptables", Type = FolderEVO.FolderType.Folder, IsMandatory = false, IsOptional = true, IsNamespaceProvider = true,
                SubFolders = new List<FolderEVO>()
            },
            new FolderEVO
            {
                FolderName = "Scripts",
                SubFolders = new List<FolderEVO>
                {
                    new FolderEVO
                    {
                        FolderName = "Runtime",
                        SubFolders = new List<FolderEVO>
                        {
                            new FolderEVO
                            {
                                FolderName = "Data",
                                SubFolders = new List<FolderEVO>
                                {
                                    new FolderEVO
                                    {
                                        FolderName = "UnityObjects", Type = FolderEVO.FolderType.UnityObjects, IsMandatory = true,
                                        IsNamespaceProvider = true
                                    },
                                    new FolderEVO
                                    {
                                        FolderName = "ValueObjects", Type = FolderEVO.FolderType.ValueObjects, IsMandatory = true,
                                        IsNamespaceProvider = true
                                    }
                                },
                                Type = FolderEVO.FolderType.Folder,
                                IsMandatory = true,
                                IsNamespaceProvider = true
                            },
                            new FolderEVO
                            {
                                FolderName = "Models",
                                Type = FolderEVO.FolderType.Models,
                                IsMandatory = true,
                                IsNamespaceProvider = true
                            },
                            new FolderEVO
                            {
                                FolderName = "Enums",
                                Type = FolderEVO.FolderType.Folder,
                                IsMandatory = false,
                                IsOptional = true,
                                IsNamespaceProvider = true
                            },
                            new FolderEVO
                            {
                                FolderName = "RootsContexts",
                                Type = FolderEVO.FolderType.RootsAndContexts,
                                IsMandatory = true,
                                IsNamespaceProvider = true
                            },
                            new FolderEVO
                            {
                                FolderName = "Signals",
                                Type = FolderEVO.FolderType.Signals,
                                IsMandatory = false,
                                IsOptional = true,
                                IsNamespaceProvider = true
                            },
                            new FolderEVO
                            {
                                FolderName = "ViewsMediators",
                                Type = FolderEVO.FolderType.ViewsAndMediators,
                                IsMandatory = true,
                                IsNamespaceProvider = true
                            },
                            new FolderEVO
                            {
                                FolderName = "Functions",
                                Type = FolderEVO.FolderType.Folder,
                                IsMandatory = true,
                                IsNamespaceProvider = true
                            },
                            new FolderEVO
                            {
                                FolderName = "Services",
                                Type = FolderEVO.FolderType.Services,
                                IsMandatory = false,
                                IsOptional = true,
                                IsNamespaceProvider = true
                            },
                            new FolderEVO
                            {
                                FolderName = "Constants",
                                Type = FolderEVO.FolderType.Folder,
                                IsMandatory = false,
                                IsOptional = true,
                                IsNamespaceProvider = true
                            },
                            new FolderEVO
                            {
                                FolderName = "Controllers",
                                Type = FolderEVO.FolderType.Controllers,
                                IsMandatory = true,
                                IsNamespaceProvider = true
                            },
                            new FolderEVO
                            {
                                FolderName = "Entities",
                                Type = FolderEVO.FolderType.Folder,
                                IsMandatory = true,
                                IsNamespaceProvider = true
                            }
                        },
                        Type = FolderEVO.FolderType.Folder,
                        IsMandatory = true,
                        IsNamespaceProvider = false
                    },
                    new FolderEVO
                    {
                        FolderName = "Editor", Type = FolderEVO.FolderType.Editor, IsMandatory = true
                    }
                },
                Type = FolderEVO.FolderType.Folder,
                IsMandatory = true,
                IsNamespaceProvider = false
            },

            new FolderEVO
            {
                FolderName = "zSubModules", Type = FolderEVO.FolderType.SubModules, IsMandatory = false, IsNamespaceProvider = false
            },
            new FolderEVO
            {
                FolderName = "zTestModules", Type = FolderEVO.FolderType.TestModules, IsMandatory = false, IsNamespaceProvider = false
            },
            new FolderEVO
            {
                FolderName = "zScreenModules", Type = FolderEVO.FolderType.ScreenModules, IsMandatory = false, IsNamespaceProvider = false
            }
        };

        public static DirectoryStructureConfig GetOrCreateConfig(string configKey)
        {
            new FlowIoCPathMigrator().MigrateIfNeeded();

            string settingsPath = CodeGeneratorStrings.CONFIG_PATH;
            var settings = AssetDatabase.LoadAssetAtPath<ED_CodeGenerator>(settingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<ED_CodeGenerator>();
                AssetDatabase.CreateAsset(settings, settingsPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"ED_CodeGenerator asset created at: {settingsPath}");
            }

            if (!settings.DirectoryStructureConfigPaths.TryGetValue(configKey, out string configPath))
            {
                throw new Exception($"Config path for key '{configKey}' not found in ED_CodeGenerator. Please add it.");
            }

            ED_TestModuleDirectoryStructure config = AssetDatabase.LoadAssetAtPath<ED_TestModuleDirectoryStructure>(configPath);

            if (config == null)
            {
                string fullPath = Path.GetDirectoryName(configPath);
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                }

                config = CreateInstance<ED_TestModuleDirectoryStructure>();
                config.InitializeDefaultFolderStructure();

                AssetDatabase.CreateAsset(config, configPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"DirectoryStructureConfig created at: {configPath}");
            }

            bool healed = config.RemoveFolderType(FolderEVO.FolderType.ScreenConfigs);
            healed |= config.MakeFolderOptional("Scriptables");

            if (healed)
            {
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
            }

            return config;
        }

        protected override void InitializeDefaultFolderStructure()
        {
            base.InitializeDefaultFolderStructure();

            var codeGenSettings = AssetDatabase.LoadAssetAtPath<ED_CodeGenerator>(CodeGeneratorStrings.CONFIG_PATH);
            if (codeGenSettings == null)
            {
                Debug.LogError($"ED_CodeGenerator asset not found at {CodeGeneratorStrings.CONFIG_PATH}");
                return;
            }

            RootFolders = new List<FolderEVO>
            {
                CreateFolder("Art", FolderEVO.FolderType.Folder, null, false, true),
                CreateFolder(codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.Prefabs], FolderEVO.FolderType.Prefabs, null,
                    true),
                CreateFolder(codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.Resources], FolderEVO.FolderType.Resources, null,
                    false, true),
                CreateFolder(codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.Scenes], FolderEVO.FolderType.Scenes, null, false,
                    true),
                CreateFolder("Scriptables", FolderEVO.FolderType.Folder, new List<FolderEVO>(), false, true, true),
                CreateFolder("Scripts", FolderEVO.FolderType.Folder, new List<FolderEVO>
                {
                    CreateFolder("Runtime", FolderEVO.FolderType.Folder, new List<FolderEVO>
                    {
                        CreateFolder("Data", FolderEVO.FolderType.Folder, new List<FolderEVO>
                        {
                            CreateFolder(codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.UnityObjects],
                                FolderEVO.FolderType.UnityObjects, null, true),
                            CreateFolder(codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.ValueObjects],
                                FolderEVO.FolderType.ValueObjects, null, true),
                        }, true),
                        CreateFolder(codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.Models], FolderEVO.FolderType.Models,
                            null, true, isNamespaceProvider: true),
                        CreateFolder("Enums", FolderEVO.FolderType.Folder, null, false, true, isNamespaceProvider: true),
                        CreateFolder(codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.RootsAndContexts],
                            FolderEVO.FolderType.RootsAndContexts, null, true, isNamespaceProvider: true),
                        CreateFolder(codeGenSettings.FolderNameFor(FolderEVO.FolderType.Signals, "Signals"),
                            FolderEVO.FolderType.Signals, null, false, true, isNamespaceProvider: true),
                        CreateFolder(codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.ViewsAndMediators],
                            FolderEVO.FolderType.ViewsAndMediators, null, true, isNamespaceProvider: true),
                        CreateFolder("Functions", FolderEVO.FolderType.Folder, null, true, isNamespaceProvider: true),
                        CreateFolder(codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.Services], FolderEVO.FolderType.Services,
                            null, false, true, isNamespaceProvider: true),
                        CreateFolder("Constants", FolderEVO.FolderType.Folder, null, false, true, isNamespaceProvider: true),
                        CreateFolder(codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.Controllers],
                            FolderEVO.FolderType.Controllers, null, true, isNamespaceProvider: true),
                        CreateFolder("Entities", FolderEVO.FolderType.Folder, null, true, isNamespaceProvider: true)
                    }, true, false, false),
                    CreateFolder(codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.Editor], FolderEVO.FolderType.Editor, null,
                        true)
                }, true, false, false),

                CreateFolder(codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.SubModules], FolderEVO.FolderType.SubModules,
                    null, false, false, false),
                CreateFolder(codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.TestModules], FolderEVO.FolderType.TestModules,
                    null, false, false, false),
                CreateFolder(codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.ScreenModules],
                    FolderEVO.FolderType.ScreenModules, null, false, false, false)
            };
        }

        protected override FolderEVO CreateFolder(string folderName, FolderEVO.FolderType folderType, List<FolderEVO> subFolders = null,
            bool isMandatory = false, bool isOptional = false,
            bool isNamespaceProvider = true)
        {
            base.CreateFolder(folderName, folderType, subFolders);
            return new FolderEVO
            {
                FolderName = folderName,
                Type = folderType,
                SubFolders = subFolders ?? new List<FolderEVO>(),
                IsMandatory = isMandatory,
                IsOptional = isOptional,
                IsNamespaceProvider = isNamespaceProvider
            };
        }

        protected override string FindFolderPathByID(FolderEVO.FolderType folderID, List<FolderEVO> folders, string basePath,
            out bool isOptional)
        {
            foreach (FolderEVO folder in folders)
            {
                string currentPath = Path.Combine(basePath, folder.FolderName);

                if (folder.Type.ToString().Equals(folderID.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    isOptional = folder.IsOptional;
                    return currentPath;
                }
                else if (folder.SubFolders != null && folder.SubFolders.Count > 0)
                {
                    string subFolderPath = FindFolderPathByID(folderID, folder.SubFolders, currentPath, out isOptional);
                    if (!string.IsNullOrEmpty(subFolderPath))
                    {
                        // A folder is only as mandatory as the branch it hangs off: one marked
                        // mandatory inside an optional parent is still absent from every module
                        // that was created without the parent, which is ordinary rather than a
                        // fault the caller should warn about.
                        isOptional |= folder.IsOptional;
                        return subFolderPath;
                    }
                }
            }

            isOptional = false;
            return string.Empty;
        }
    }
}
#endif