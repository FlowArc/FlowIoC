#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using FlowIoC.BaseModule.ProjectPaths;
using FlowIoC.Editor.CodeGenerator.Extensions;
using FlowIoC.Editor.CodeGenerator.Menus.Module;
using FlowIoC.Editor.Config.ModuleConfig;
using FlowIoC.Editor.Migration;
using FlowIoC.Editor.Modules;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator
{
    [CreateAssetMenu(menuName = "FlowIoC/Editor/CodeGenerator/CodeGeneratorSettings", fileName = "CodeGeneratorSettings", order = 1)]
    public class CodeGeneratorSettings : ScriptableObject
    {
        public List<AssemblyDefinitionAsset> AssemblyDefinitions;

        [HideInInspector] [SerializeField] public SerializableDictionary<FolderConfig.FolderType, string> DirectoryStructureConfigMap =
            new SerializableDictionary<FolderConfig.FolderType, string>
            {
                {FolderConfig.FolderType.SubModules, "zSubModules"},
                {FolderConfig.FolderType.TestModules, "zTestModules"},
                {FolderConfig.FolderType.ScreenModules, "zScreenModules"},
                {FolderConfig.FolderType.ViewsAndMediators, "ViewsMediators"},
                {FolderConfig.FolderType.ScreenConfigs, "ScreenConfigs"},
                {FolderConfig.FolderType.RootsAndContexts, "RootsContexts"},
                {FolderConfig.FolderType.Services, "Services"},
                {FolderConfig.FolderType.Systems, "Systems"},
                {FolderConfig.FolderType.Signals, "Signals"},
                {FolderConfig.FolderType.Controllers, "Controllers"},
                {FolderConfig.FolderType.Models, "Models"},
                {FolderConfig.FolderType.UnityObjects, "UnityObjects"},
                {FolderConfig.FolderType.ValueObjects, "ValueObjects"},
                {FolderConfig.FolderType.Editor, "Editor"},
                {FolderConfig.FolderType.Resources, "Resources"},
                {FolderConfig.FolderType.Prefabs, "Prefabs"},
                {FolderConfig.FolderType.Scenes, "Scenes"},
                {FolderConfig.FolderType.Shared, "Shared"},
                {FolderConfig.FolderType.SharedUnityObjects, "UnityObjects"},
                {FolderConfig.FolderType.SharedValueObjects, "ValueObjects"},
                {FolderConfig.FolderType.SharedEnums, "Enums"},
                {FolderConfig.FolderType.SharedConstants, "Constants"},
                {FolderConfig.FolderType.SharedSignals, "Signals"}
            };

        [HideInInspector] [SerializeField] public SerializableDictionary<string, string> DirectoryStructureConfigPaths =
            CreateDefaultDirectoryStructureConfigPaths();

        private static SerializableDictionary<string, string> CreateDefaultDirectoryStructureConfigPaths()
        {
            var paths = new FlowIoCProjectPaths();

            return new SerializableDictionary<string, string>
            {
                {"Main", paths.DirectoryStructureConfig("Main")},
                {"Screen", paths.DirectoryStructureConfig("Screen")},
                {"Test", paths.DirectoryStructureConfig("Test")}
            };
        }

        /// <summary>
        /// The folder name for a type, falling back when the settings asset predates it.
        /// This asset lives in the consuming project and is serialized once, so a folder type
        /// added in a later FlowIoC version is simply absent from every existing project's
        /// copy. Indexing the dictionary directly throws there and takes module creation down
        /// with it, so every newly added type must be read through here.
        /// </summary>
        public string FolderNameFor(FolderConfig.FolderType folderType, string fallback)
        {
            return DirectoryStructureConfigMap.TryGetValue(folderType, out string folderName)
                   && !string.IsNullOrEmpty(folderName)
                ? folderName
                : fallback;
        }

        public static void CreateConfig()
        {
            new FlowIoCPathMigrator().MigrateIfNeeded();

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

        public void ApplyConfiguredFolderNames()
        {
            string modulesPath = Path.Combine(Application.dataPath, "Modules");
            if (!Directory.Exists(modulesPath)) return;

            var folderOperations = new List<(string oldPath, string newPath, FolderConfig.FolderType type)>();
            CollectFolderOperations(folderOperations);
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

        /// <summary>
        /// A folder's type used to be decided by probing it for a marker file. Now every module
        /// in the index is asked directly: for each FolderType, a recorded FolderGuid is resolved
        /// through the AssetDatabase and compared to the configured name, because a GUID is the
        /// one thing that survives a rename a name lookup cannot follow. When no GUID has been
        /// recorded yet, or the one on record no longer resolves, this falls back to finding the
        /// folder by its configured name - which cannot detect a pending rename, only heal the
        /// map for the next one - and records what it finds so the same module never needs the
        /// fallback again.
        /// </summary>
        private void CollectFolderOperations(List<(string oldPath, string newPath, FolderConfig.FolderType type)> operations)
        {
            FlowIoCModuleIndex index = new ModuleIndexProvider().LoadOrCreate();
            IAssetPaths assetPaths = new AssetDatabasePaths();
            ModuleRegistry registry = new ModuleRegistry(index, assetPaths);
            ModuleAssetPathResolver pathResolver = new ModuleAssetPathResolver();
            FolderRenamePlanner renamePlanner = new FolderRenamePlanner();
            DirectoryStructureConfigProvider configProvider = new DirectoryStructureConfigProvider();

            string dataPath = Application.dataPath.Replace('\\', '/');
            bool indexDirty = false;

            foreach (ModuleDescriptor module in registry.Modules)
            {
                string moduleAssetPath = registry.PathOf(module);
                if (string.IsNullOrEmpty(moduleAssetPath)) continue;

                string modulePath = pathResolver.ToAbsolutePath(moduleAssetPath);
                if (string.IsNullOrEmpty(modulePath)) continue;

                DirectoryStructureConfig directoryConfig = configProvider.ConfigFor(module.Kind);

                foreach (KeyValuePair<FolderConfig.FolderType, string> kvp in DirectoryStructureConfigMap)
                {
                    FolderConfig.FolderType type = kvp.Key;
                    string configuredName = kvp.Value;
                    if (string.IsNullOrEmpty(configuredName)) continue;

                    bool guidWentStale = false;

                    if (module.TryGetFolderGuid(type, out string guid))
                    {
                        string currentAssetPath = assetPaths.PathOf(guid);
                        if (!string.IsNullOrEmpty(currentAssetPath))
                        {
                            string currentAbsolutePath = pathResolver.ToAbsolutePath(currentAssetPath);
                            if (renamePlanner.TryPlanRename(currentAbsolutePath, configuredName, out string newAbsolutePath))
                            {
                                operations.Add((currentAbsolutePath, newAbsolutePath, type));
                            }

                            continue;
                        }

                        // A recorded GUID that no longer resolves - the folder deleted, or moved out
                        // of the project outside Unity - used to skip this type outright, which took
                        // the fallback below with it and left the map dead for good. Falling through
                        // instead lets the name lookup find the folder again and RecordFolderGuid
                        // overwrite the dead entry.
                        guidWentStale = true;
                    }

                    if (directoryConfig == null) continue;

                    string expectedAbsolutePath = directoryConfig.FindFullFolderPathByID(type, modulePath, out bool isOptional);
                    if (string.IsNullOrEmpty(expectedAbsolutePath)) continue;

                    if (!Directory.Exists(expectedAbsolutePath))
                    {
                        // About half the tracked folder types are optional, and a module that never
                        // had one is the normal case rather than a fault. Warning for each of them on
                        // every pass would bury the misses that do mean something.
                        if (isOptional) continue;

                        LogFallbackMiss(module, type, guidWentStale
                            ? "its recorded GUID no longer resolves to a folder and nothing exists at " +
                              $"'{expectedAbsolutePath}' either, so the folder cannot be found again."
                            : $"expected at '{expectedAbsolutePath}' but nothing exists there. A later rename of " +
                              "this folder type will not follow it until it is found - if it was never created " +
                              "for this module, this can be ignored.");
                        continue;
                    }

                    // NamespaceUtility.GetUnityAssetPath only resolves paths under Assets/. An
                    // embedded-package module (ModuleIndexRebuilder also scans Packages/*/Modules)
                    // would otherwise be misread as the Assets root and have that folder's GUID
                    // recorded by mistake, so skip healing it here rather than risk corrupting the
                    // index over it.
                    if (!expectedAbsolutePath.Replace('\\', '/').StartsWith(dataPath, StringComparison.Ordinal))
                    {
                        LogFallbackMiss(module, type,
                            $"'{expectedAbsolutePath}' sits outside the Assets folder (an embedded package " +
                            "module), which this fallback does not resolve.");
                        continue;
                    }

                    string expectedAssetPath = NamespaceUtility.GetUnityAssetPath(expectedAbsolutePath);
                    string foundGuid = assetPaths.GuidOf(expectedAssetPath);
                    if (string.IsNullOrEmpty(foundGuid))
                    {
                        LogFallbackMiss(module, type,
                            $"the AssetDatabase has no GUID yet for '{expectedAbsolutePath}'. Try again after " +
                            "an AssetDatabase.Refresh().");
                        continue;
                    }

                    module.RecordFolderGuid(type, foundGuid);
                    indexDirty = true;
                }
            }

            if (indexDirty)
            {
                EditorUtility.SetDirty(index);
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>
        /// The fallback exists to heal a module whose GUID was never recorded, and a heal that
        /// silently never happens is indistinguishable from one that had nothing to do. This is
        /// unconditional (not FlowLogger, which compiles out) so the gap is visible by default -
        /// which is also why the caller keeps quiet about folder types marked optional, whose
        /// absence is ordinary rather than a gap.
        /// </summary>
        private void LogFallbackMiss(ModuleDescriptor module, FolderConfig.FolderType type, string detail)
        {
            Debug.LogWarning($"<color=cyan>FlowIoC:</color> could not record a GUID for the '{type}' folder of " +
                             $"module '{module.Name}' - {detail}");
        }
    }
}
#endif