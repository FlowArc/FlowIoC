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
    [CreateAssetMenu(fileName = "MainModuleDirectoryStructureConfig",
        menuName = "FlowIoC/Editor/CodeGenerator/MainModule Directory Structure Config")]
    public class MainModuleDirectoryStructureConfig : DirectoryStructureConfig
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
                        FolderName = "ScreenConfigs", Type = FolderConfig.FolderType.ScreenConfigs, IsMandatory = false, IsOptional = true,
                        IsNamespaceProvider = true
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
                                FolderName = "Data",
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
                                IsMandatory = true,
                                IsNamespaceProvider = true
                            },
                            new FolderConfig
                            {
                                FolderName = "Models",
                                Type = FolderConfig.FolderType.Folder,
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
                                Type = FolderConfig.FolderType.Signals,
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
                                Type = FolderConfig.FolderType.Folder,
                                IsMandatory = false,
                                IsOptional = true,
                                IsNamespaceProvider = true
                            },
                            new FolderConfig
                            {
                                FolderName = "Systems",
                                Type = FolderConfig.FolderType.Systems,
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
                        FolderName = "Editor", Type = FolderConfig.FolderType.Folder, IsMandatory = true
                    },
                    new FolderConfig
                    {
                        FolderName = "Shared",
                        SubFolders = new List<FolderConfig>
                        {
                            new FolderConfig
                            {
                                FolderName = "Data",
                                SubFolders = new List<FolderConfig>
                                {
                                    new FolderConfig
                                    {
                                        FolderName = "UnityObjects", Type = FolderConfig.FolderType.SharedUnityObjects, IsMandatory = true,
                                        IsNamespaceProvider = true
                                    },
                                    new FolderConfig
                                    {
                                        FolderName = "ValueObjects", Type = FolderConfig.FolderType.SharedValueObjects, IsMandatory = true,
                                        IsNamespaceProvider = true
                                    }
                                },
                                Type = FolderConfig.FolderType.Folder,
                                IsMandatory = true,
                                IsNamespaceProvider = true
                            },
                            new FolderConfig
                            {
                                FolderName = "Enums",
                                Type = FolderConfig.FolderType.SharedEnums,
                                IsMandatory = true,
                                IsNamespaceProvider = true
                            },
                            new FolderConfig
                            {
                                FolderName = "Constants",
                                Type = FolderConfig.FolderType.SharedConstants,
                                IsMandatory = true,
                                IsNamespaceProvider = true
                            }
                        },
                        Type = FolderConfig.FolderType.Shared,
                        IsMandatory = false,
                        IsOptional = true,
                        IsNamespaceProvider = true
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
            var settings = AssetDatabase.LoadAssetAtPath<CodeGeneratorSettings>(settingsPath);
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

            MainModuleDirectoryStructureConfig config = AssetDatabase.LoadAssetAtPath<MainModuleDirectoryStructureConfig>(configPath);

            if (config == null)
            {
                string fullPath = Path.GetDirectoryName(configPath);
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                }

                config = CreateInstance<MainModuleDirectoryStructureConfig>();
                config.InitializeDefaultFolderStructure();

                AssetDatabase.CreateAsset(config, configPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"DirectoryStructureConfig created at: {configPath}");
            }

            if (config.EnsureSharedBranch(settings))
            {
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
            }

            return config;
        }

        /// <summary>
        /// Adds the Shared branch to a config asset written before the branch existed, and returns
        /// whether it changed anything.
        ///
        /// The structure above is only the default a brand new asset is stamped with; every
        /// project that already ran the code generator has its own serialized copy, which
        /// <see cref="GetOrCreateConfig"/> loads untouched. Without this the Shared folder would
        /// never appear in an existing project, and asking people to delete the asset to get it
        /// would throw away whatever they had customized in the inspector. So this only ever
        /// appends, and only when the project has no Shared folder at all.
        /// </summary>
        internal bool EnsureSharedBranch(CodeGeneratorSettings codeGenSettings)
        {
            if (codeGenSettings == null || RootFolders == null) return false;
            if (ContainsFolderType(RootFolders, FolderConfig.FolderType.Shared)) return false;

            FolderConfig scripts = FindFolderByName(RootFolders, "Scripts");
            if (scripts == null)
            {
                Debug.LogWarning("<color=cyan>FlowIoC:</color> the main module directory structure has no 'Scripts' folder, so the " +
                                 "Shared branch could not be added to it. Add a Shared folder to the config asset by hand if this " +
                                 "module layout is meant to have one.");
                return false;
            }

            scripts.SubFolders ??= new List<FolderConfig>();
            scripts.SubFolders.Add(
                CreateFolder(codeGenSettings.FolderNameFor(FolderConfig.FolderType.Shared, "Shared"), FolderConfig.FolderType.Shared,
                    new List<FolderConfig>
                    {
                        CreateFolder("Data", FolderConfig.FolderType.Folder, new List<FolderConfig>
                        {
                            CreateFolder(codeGenSettings.FolderNameFor(FolderConfig.FolderType.SharedUnityObjects, "UnityObjects"),
                                FolderConfig.FolderType.SharedUnityObjects, null, true),
                            CreateFolder(codeGenSettings.FolderNameFor(FolderConfig.FolderType.SharedValueObjects, "ValueObjects"),
                                FolderConfig.FolderType.SharedValueObjects, null, true)
                        }, true),
                        CreateFolder(codeGenSettings.FolderNameFor(FolderConfig.FolderType.SharedEnums, "Enums"),
                            FolderConfig.FolderType.SharedEnums, null, true),
                        CreateFolder(codeGenSettings.FolderNameFor(FolderConfig.FolderType.SharedConstants, "Constants"),
                            FolderConfig.FolderType.SharedConstants, null, true)
                    }, false, true));

            RegisterSharedFolderNames(codeGenSettings);

            return true;
        }

        /// <summary>
        /// Puts the Shared folder types into the settings map, which is what makes their folders
        /// rename-tracked: ModuleIndexRegistrar records a GUID per type in that map, and
        /// ApplyConfiguredFolderNames renames per type in it. A settings asset written before
        /// these types existed has none of them, so the branch this method accompanies would
        /// otherwise be created but never followed through a rename.
        ///
        /// This runs only on the pass that adds the branch, never on every load. Removing an entry
        /// from the settings inspector is a deliberate act, and a heal that ran unconditionally
        /// would put it straight back.
        /// </summary>
        private void RegisterSharedFolderNames(CodeGeneratorSettings codeGenSettings)
        {
            var defaults = new Dictionary<FolderConfig.FolderType, string>
            {
                {FolderConfig.FolderType.Shared, "Shared"},
                {FolderConfig.FolderType.SharedUnityObjects, "UnityObjects"},
                {FolderConfig.FolderType.SharedValueObjects, "ValueObjects"},
                {FolderConfig.FolderType.SharedEnums, "Enums"},
                {FolderConfig.FolderType.SharedConstants, "Constants"}
            };

            bool added = false;

            foreach (KeyValuePair<FolderConfig.FolderType, string> entry in defaults)
            {
                if (codeGenSettings.DirectoryStructureConfigMap.ContainsKey(entry.Key)) continue;

                codeGenSettings.DirectoryStructureConfigMap.Add(entry.Key, entry.Value);
                added = true;
            }

            if (!added) return;

            EditorUtility.SetDirty(codeGenSettings);
        }

        private bool ContainsFolderType(List<FolderConfig> folders, FolderConfig.FolderType folderType)
        {
            if (folders == null) return false;

            foreach (FolderConfig folder in folders)
            {
                if (folder.Type == folderType) return true;
                if (ContainsFolderType(folder.SubFolders, folderType)) return true;
            }

            return false;
        }

        private FolderConfig FindFolderByName(List<FolderConfig> folders, string folderName)
        {
            if (folders == null) return null;

            foreach (FolderConfig folder in folders)
            {
                if (string.Equals(folder.FolderName, folderName, StringComparison.OrdinalIgnoreCase)) return folder;

                FolderConfig found = FindFolderByName(folder.SubFolders, folderName);
                if (found != null) return found;
            }

            return null;
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
                        FolderConfig.FolderType.ScreenConfigs, null, false, true)
                }, false, true, true),
                CreateFolder("Scripts", FolderConfig.FolderType.Folder, new List<FolderConfig>
                {
                    CreateFolder("Runtime", FolderConfig.FolderType.Folder, new List<FolderConfig>
                    {
                        CreateFolder("Data", FolderConfig.FolderType.Folder, new List<FolderConfig>
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
                            FolderConfig.FolderType.RootsAndContexts, null, true,
                            isNamespaceProvider: true),
                        CreateFolder(codeGenSettings.FolderNameFor(FolderConfig.FolderType.Signals, "Signals"),
                            FolderConfig.FolderType.Signals, null, false, true),
                        CreateFolder(codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.ViewsAndMediators],
                            FolderConfig.FolderType.ViewsAndMediators, new List<FolderConfig>
                            {
                                CreateFolder(codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.ScreenViews],
                                    FolderConfig.FolderType.ScreenViews, null, true)
                            }, true),
                        CreateFolder("Functions", FolderConfig.FolderType.Folder, null, true, isNamespaceProvider: true),
                        CreateFolder(codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.Services], FolderConfig.FolderType.Services,
                            null, false, true, isNamespaceProvider: true),
                        CreateFolder(codeGenSettings.FolderNameFor(FolderConfig.FolderType.Systems, "Systems"), FolderConfig.FolderType.Systems,
                            null, false, true, isNamespaceProvider: true),
                        CreateFolder("Constants", FolderConfig.FolderType.Folder, null, false, true, isNamespaceProvider: true),
                        CreateFolder(codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.Controllers],
                            FolderConfig.FolderType.Controllers, null, true, isNamespaceProvider: true),
                        CreateFolder("Entities", FolderConfig.FolderType.Folder, null, true, isNamespaceProvider: true)
                    }, true, false, false),
                    CreateFolder(codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.Editor], FolderConfig.FolderType.Editor, null,
                        true),
                    // Shared is a sibling of Runtime rather than a folder inside it because it
                    // becomes its own assembly: what a module wants to publish to a screen or a
                    // sub module is the data, not the Models and Commands that Runtime holds.
                    // Unlike Runtime it is a namespace provider, so a shared value object lands in
                    // <Module>.Shared.Data.ValueObjects and cannot collide with the Runtime type
                    // of the same name sitting in <Module>.Data.ValueObjects.
                    CreateFolder(codeGenSettings.FolderNameFor(FolderConfig.FolderType.Shared, "Shared"), FolderConfig.FolderType.Shared,
                        new List<FolderConfig>
                        {
                            CreateFolder("Data", FolderConfig.FolderType.Folder, new List<FolderConfig>
                            {
                                CreateFolder(codeGenSettings.FolderNameFor(FolderConfig.FolderType.SharedUnityObjects, "UnityObjects"),
                                    FolderConfig.FolderType.SharedUnityObjects, null, true),
                                CreateFolder(codeGenSettings.FolderNameFor(FolderConfig.FolderType.SharedValueObjects, "ValueObjects"),
                                    FolderConfig.FolderType.SharedValueObjects, null, true)
                            }, true),
                            CreateFolder(codeGenSettings.FolderNameFor(FolderConfig.FolderType.SharedEnums, "Enums"),
                                FolderConfig.FolderType.SharedEnums, null, true),
                            CreateFolder(codeGenSettings.FolderNameFor(FolderConfig.FolderType.SharedConstants, "Constants"),
                                FolderConfig.FolderType.SharedConstants, null, true)
                        }, false, true)
                }, true, false, false),

                CreateFolder(codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.SubModules], FolderConfig.FolderType.SubModules,
                    null, false, true, false),
                CreateFolder(codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.TestModules], FolderConfig.FolderType.TestModules,
                    null, false, true, false),
                CreateFolder(codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.ScreenModules],
                    FolderConfig.FolderType.ScreenModules, null, false, true, false)
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
                        // A folder is only as mandatory as the branch it hangs off. The Shared
                        // subfolders are each mandatory within Shared - ticking Shared lays all of
                        // them down at once - but Shared itself is optional, so a module created
                        // without it has no Data/UnityObjects under Shared and that is ordinary
                        // rather than a fault. Without this the caller that warns about a missing
                        // folder would warn once per Shared subfolder for every such module.
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