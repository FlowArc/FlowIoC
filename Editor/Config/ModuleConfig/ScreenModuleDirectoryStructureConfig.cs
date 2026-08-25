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
    [CreateAssetMenu(fileName = "ScreenModuleDirectoryStructureConfig",
        menuName = "FlowIoC/Editor/CodeGenerator/ScreenModule Directory Structure Config")]
    public class ScreenModuleDirectoryStructureConfig : DirectoryStructureConfig
    {
        [field: SerializeReference]
        protected internal override List<FolderConfig> RootFolders { get; protected set; } = new List<FolderConfig>
        {
            new FolderConfig
            {
                FolderName = "Art", Type = FolderConfig.FolderType.Folder, IsMandatory = false, IsOptional = true, IsNamespaceProvider = true
            },
            new FolderConfig
            {
                FolderName = "Prefabs", Type = FolderConfig.FolderType.Prefabs, IsMandatory = true, IsNamespaceProvider = true
            },
            new FolderConfig
            {
                FolderName = "Resources", Type = FolderConfig.FolderType.Resources, IsMandatory = false, IsOptional = true, IsNamespaceProvider = true
            },
            new FolderConfig
            {
                FolderName = "Scenes", Type = FolderConfig.FolderType.Scenes, IsMandatory = false, IsOptional = true, IsNamespaceProvider = true
            },
            new FolderConfig()
            {
                FolderName = "Scriptables", Type = FolderConfig.FolderType.Folder, IsMandatory = false, IsOptional = true, IsNamespaceProvider = true,
                SubFolders = new List<FolderConfig>()
                {
                    new FolderConfig()
                    {
                        FolderName = "ScreenConfigs", Type = FolderConfig.FolderType.ScreenConfigs, IsMandatory = false, IsOptional = true
                    }
                }
            },
            new FolderConfig
            {
                FolderName = "Scripts",
                SubFolders = new List<FolderConfig>
                {
                    new FolderConfig
                    {
                        FolderName = "Runtime",
                        SubFolders = new List<FolderConfig>
                        {
                            new FolderConfig
                            {
                                FolderName = "Datas",
                                SubFolders = new List<FolderConfig>
                                {
                                    new FolderConfig
                                    {
                                        FolderName = "UnityObjects", Type = FolderConfig.FolderType.UnityObjects, IsMandatory = true,
                                        IsNamespaceProvider = true
                                    },
                                    new FolderConfig
                                    {
                                        FolderName = "ValueObjects", Type = FolderConfig.FolderType.ValueObjects, IsMandatory = true,
                                        IsNamespaceProvider = true
                                    }
                                },
                                Type = FolderConfig.FolderType.Folder,
                                IsMandatory = true, IsNamespaceProvider = true
                            },
                            new FolderConfig
                            {
                                FolderName = "Models",
                                Type = FolderConfig.FolderType.Models,
                                IsMandatory = true,
                                IsNamespaceProvider = true
                            },
                            new FolderConfig
                            {
                                FolderName = "Enums",
                                Type = FolderConfig.FolderType.Folder,
                                IsMandatory = false,
                                IsOptional = true,
                                IsNamespaceProvider = true
                            },
                            new FolderConfig
                            {
                                FolderName = "RootsContexts",
                                Type = FolderConfig.FolderType.RootsAndContexts,
                                IsMandatory = true,
                                IsNamespaceProvider = true
                            },
                            new FolderConfig
                            {
                                FolderName = "Signals",
                                Type = FolderConfig.FolderType.Folder,
                                IsMandatory = false,
                                IsOptional = true,
                                IsNamespaceProvider = true
                            },
                            new FolderConfig
                            {
                                FolderName = "ViewsMediators",
                                SubFolders = new List<FolderConfig>
                                {
                                    new FolderConfig
                                    {
                                        FolderName = "Screens",
                                        Type = FolderConfig.FolderType.ScreenViews,
                                        IsMandatory = true
                                    },
                                },
                                Type = FolderConfig.FolderType.ViewsAndMediators,
                                IsMandatory = true,
                                IsNamespaceProvider = true
                            },
                            new FolderConfig
                            {
                                FolderName = "Functions",
                                Type = FolderConfig.FolderType.Folder,
                                IsMandatory = true,
                                IsNamespaceProvider = true
                            },
                            new FolderConfig
                            {
                                FolderName = "Services",
                                Type = FolderConfig.FolderType.Services,
                                IsMandatory = false,
                                IsOptional = true,
                                IsNamespaceProvider = true
                            },
                            new FolderConfig
                            {
                                FolderName = "Constants",
                                Type = FolderConfig.FolderType.Folder,
                                IsMandatory = false,
                                IsOptional = true,
                                IsNamespaceProvider = true
                            },
                            new FolderConfig
                            {
                                FolderName = "Controllers",
                                Type = FolderConfig.FolderType.Controllers,
                                IsMandatory = true,
                                IsNamespaceProvider = true
                            },
                            new FolderConfig
                            {
                                FolderName = "Entities",
                                Type = FolderConfig.FolderType.Folder,
                                IsMandatory = true,
                                IsNamespaceProvider = true
                            }
                        },
                        Type = FolderConfig.FolderType.Folder,
                        IsMandatory = true,
                        IsNamespaceProvider = false
                    },
                    new FolderConfig
                    {
                        FolderName = "Editor", Type = FolderConfig.FolderType.Editor, IsMandatory = true, IsNamespaceProvider = true
                    }
                },
                Type = FolderConfig.FolderType.Folder,
                IsMandatory = true,
                IsNamespaceProvider = false
            },

            new FolderConfig
            {
                FolderName = "zSubModules", Type = FolderConfig.FolderType.SubModules, IsMandatory = false, IsOptional = true,
                IsNamespaceProvider = false
            },
            new FolderConfig
            {
                FolderName = "zTestModules", Type = FolderConfig.FolderType.TestModules, IsMandatory = false, IsOptional = true,
                IsNamespaceProvider = false
            },
            new FolderConfig
            {
                FolderName = "zScreenModules", Type = FolderConfig.FolderType.ScreenModules, IsMandatory = false, IsOptional = true,
                IsNamespaceProvider = false
            }
        };

        public static DirectoryStructureConfig GetOrCreateConfig(string configKey)
        {
            new FlowIoCPathMigrator().MigrateIfNeeded();

            string settingsPath = CodeGeneratorStrings.CONFIG_PATH;
            CodeGeneratorSettings settings = AssetDatabase.LoadAssetAtPath<CodeGeneratorSettings>(settingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<CodeGeneratorSettings>();
                AssetDatabase.CreateAsset(settings, settingsPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"CodeGeneratorSettings asset created at: {settingsPath}");
            }


            if (!settings.DirectoryStructureConfigPaths.TryGetValue(configKey, out string configPath))
            {
                throw new Exception($"Config path for key '{configKey}' not found in CodeGeneratorSettings. Please add it.");
            }

            ScreenModuleDirectoryStructureConfig config = AssetDatabase.LoadAssetAtPath<ScreenModuleDirectoryStructureConfig>(configPath);

            if (config == null)
            {
                string fullPath = Path.GetDirectoryName(configPath);
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                }

                config = CreateInstance<ScreenModuleDirectoryStructureConfig>();
                config.InitializeDefaultFolderStructure();

                AssetDatabase.CreateAsset(config, configPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"DirectoryStructureConfig created at: {configPath}");
            }

            return config;
        }


        protected override void InitializeDefaultFolderStructure()
        {
            base.InitializeDefaultFolderStructure();

            var codeGenSettings = AssetDatabase.LoadAssetAtPath<CodeGeneratorSettings>(CodeGeneratorStrings.CONFIG_PATH);
            if (codeGenSettings == null)
            {
                Debug.LogError($"CodeGeneratorSettings asset not found at {CodeGeneratorStrings.CONFIG_PATH}");
                return;
            }

            RootFolders = new List<FolderConfig>
            {
                CreateFolder("Art", FolderConfig.FolderType.Folder, null, false, true),
                CreateFolder(codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.Prefabs], FolderConfig.FolderType.Prefabs, null,
                    true),
                CreateFolder(codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.Resources], FolderConfig.FolderType.Resources, null,
                    false, true),
                CreateFolder(codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.Scenes], FolderConfig.FolderType.Scenes, null, false,
                    true),
                CreateFolder("Scriptables", FolderConfig.FolderType.Folder, new List<FolderConfig>
                {
                    CreateFolder(codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.ScreenConfigs],
                        FolderConfig.FolderType.ScreenConfigs, null, true, false, true)
                }, true, false, true),
                CreateFolder("Scripts", FolderConfig.FolderType.Folder, new List<FolderConfig>
                {
                    CreateFolder("Runtime", FolderConfig.FolderType.Folder, new List<FolderConfig>
                    {
                        CreateFolder("Datas", FolderConfig.FolderType.Folder, new List<FolderConfig>
                        {
                            CreateFolder(codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.UnityObjects],
                                FolderConfig.FolderType.UnityObjects, null, true),
                            CreateFolder(codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.ValueObjects],
                                FolderConfig.FolderType.ValueObjects, null, true),
                        }, true),
                        CreateFolder(codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.Models], FolderConfig.FolderType.Models,
                            null, true),
                        CreateFolder("Enums", FolderConfig.FolderType.Folder, null, false, true),
                        CreateFolder(codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.RootsAndContexts],
                            FolderConfig.FolderType.RootsAndContexts, null, true),
                        CreateFolder("Signals", FolderConfig.FolderType.Folder, null, false, true),
                        CreateFolder(codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.ViewsAndMediators],
                            FolderConfig.FolderType.ViewsAndMediators, new List<FolderConfig>
                            {
                                CreateFolder(codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.ScreenViews],
                                    FolderConfig.FolderType.ScreenViews, null, true)
                            }, true, isNamespaceProvider: true),
                        CreateFolder("Functions", FolderConfig.FolderType.Folder, null, true),
                        CreateFolder(codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.Services], FolderConfig.FolderType.Services,
                            null, false, true),
                        CreateFolder("Constants", FolderConfig.FolderType.Folder, null, true),
                        CreateFolder(codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.Controllers],
                            FolderConfig.FolderType.Controllers, null, true, isNamespaceProvider: true),
                        CreateFolder("Entities", FolderConfig.FolderType.Folder, null, true)
                    }, true, false, false),
                    CreateFolder(codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.Editor], FolderConfig.FolderType.Editor, null,
                        true)
                }, true, false, false),
                CreateFolder(codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.SubModules], FolderConfig.FolderType.SubModules,
                    null, false, true, false),
                CreateFolder(codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.TestModules], FolderConfig.FolderType.TestModules,
                    null, false, true, false),
                CreateFolder(codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.ScreenModules],
                    FolderConfig.FolderType.ScreenModules, null, false, true, false),
            };
        }

        protected override FolderConfig CreateFolder(string folderName, FolderConfig.FolderType folderType, List<FolderConfig> subFolders = null,
            bool isMandatory = false, bool isOptional = false,
            bool isNamespaceProvider = true)
        {
            base.CreateFolder(folderName, folderType, subFolders);
            return new FolderConfig
            {
                FolderName = folderName,
                Type = folderType,
                SubFolders = subFolders ?? new List<FolderConfig>(),
                IsMandatory = isMandatory,
                IsOptional = isOptional,
                IsNamespaceProvider = isNamespaceProvider
            };
        }

        protected override string FindFolderPathByID(FolderConfig.FolderType folderID, List<FolderConfig> folders, string basePath,
            out bool isOptional)
        {
            foreach (FolderConfig folder in folders)
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