#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using FlowIoC.ConsoleModule;
using FlowIoC.Editor.CodeGenerator.Menus.Module.CreateModule;
using FlowIoC.Editor.Config.ModuleConfig;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration
{
    internal partial class ModuleGenerator
    {
        private const string MODULE_GENERATION_WORKING = "module_generation_working";

        private const string ROOTS_CONTEXTS_WARNING = "Roots&Contexts folder not found!";
        private const string SIGNALS_WARNING = "Signals folder not found!";
        private const string PARENT_MODULE_REQUIRED_TITLE = "Parent Module Required";
        private const string PARENT_MODULE_REQUIRED_MESSAGE = "Please select a parent module";

        private const string SELECTED_MODULE_TYPE = "SELECTED_MODULE_TYPE";
        private const string KEY_FILE_NAME = "file-name";
        private const string KEY_MODULE_NAME = "file-name";
        private const string KEY_ROOT_NAME = "root-name";
        private const string KEY_PARENT_FOLDER_PATH = "parent-folder-path";
        private const string SCREEN_PREFAB_PATH = "screen-prefab-path";
        private const string KEY_VIEW_NAMESPACE = "view-namespace";
        private const string KEY_CONTEXT_NAMESPACE = "context-namespace";
        private const string KEY_SCREEN_NAME = "screen-scene-name";
        private const string KEY_SCENE_PATH = "scene-path";
        private const string BOOL_CREATE_SCREEN = "create-screen";

        private const string PREF_KEY_SCREEN_PREFAB_PATH = "MY_SCREEN_PREFAB_PATH";
        private const string PREF_KEY_SCREEN_CONFIG_PATH = "MY_SCREEN_CONFIG_PATH";

        private static ModuleType _selectedModuleType;

        public static void CreateModuleStructure(
            string moduleName,
            string parentModulePath,
            ModuleType selectedModuleType,
            List<FolderConfig> selectedOptionalFolders,
            Dictionary<ModuleType, DirectoryStructureConfig> directoryConfigMap,
            List<string> actionNames,
            bool createRoot,
            bool createContext,
            bool createSignals,
            bool createScreen,
            ScreenConfigData screenConfigData = null
        )
        {
            EditorPrefs.SetBool(MODULE_GENERATION_WORKING, true);
            _selectedModuleType = selectedModuleType;
            EditorPrefs.SetInt(SELECTED_MODULE_TYPE, (int) selectedModuleType);

            if (string.IsNullOrEmpty(parentModulePath))
            {
                EditorUtility.DisplayDialog(PARENT_MODULE_REQUIRED_TITLE, PARENT_MODULE_REQUIRED_MESSAGE, "OK");
                return;
            }

            CodeGeneratorSettings codeGenSettings = AssetDatabase.LoadAssetAtPath<CodeGeneratorSettings>(CodeGeneratorStrings.CONFIG_PATH);
            if (codeGenSettings == null)
            {
                Debug.LogError($"CodeGeneratorSettings asset not found. Please ensure it exists at {CodeGeneratorStrings.CONFIG_PATH}.");
                return;
            }

            string subModulesFolderName = codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.SubModules];
            string testModulesFolderName = codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.TestModules];
            string screenModulesFolderName = codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.ScreenModules];

            string resolvedModuleKind = selectedModuleType switch
            {
                ModuleType.Main when parentModulePath != Path.Combine(Application.dataPath, "Modules") => "Sub",
                ModuleType.Main when parentModulePath == Path.Combine(Application.dataPath, "Modules") => "Main",
                _ => selectedModuleType.ToString()
            };

            string subDirectory = selectedModuleType switch
            {
                ModuleType.Test => testModulesFolderName,
                ModuleType.Screen => screenModulesFolderName,
                _ => resolvedModuleKind == "Sub" ? subModulesFolderName : string.Empty
            };

            string modulePath = string.IsNullOrEmpty(subDirectory)
                ? Path.Combine(parentModulePath, $"{moduleName}Module")
                : Path.Combine(parentModulePath, subDirectory, $"{moduleName}Module");

            CreateFoldersRecursively(modulePath, directoryConfigMap[selectedModuleType].RootFolders, selectedOptionalFolders);

            CreateAndUpdateModules(
                moduleName,
                modulePath,
                selectedModuleType,
                selectedOptionalFolders,
                directoryConfigMap,
                codeGenSettings,
                actionNames,
                createRoot,
                createContext,
                createSignals,
                createScreen,
                screenConfigData,
                testModulesFolderName
            );
        }

        private static void CreateAndUpdateModules(
            string moduleName,
            string modulePath,
            ModuleType selectedModuleType,
            List<FolderConfig> selectedOptionalFolders,
            Dictionary<ModuleType, DirectoryStructureConfig> directoryConfigMap,
            CodeGeneratorSettings codeGenSettings,
            List<string> actionNames,
            bool createRoot,
            bool createContext,
            bool createSignals,
            bool createScreen,
            ScreenConfigData screenConfigData,
            string testModulesFolderName
        )
        {
            if (selectedModuleType == ModuleType.Main || selectedModuleType == ModuleType.Test)
            {
                string finalModuleName = moduleName + "Module";
                string asmdefPath = Path.Combine(modulePath, finalModuleName + ".asmdef");
                CreateAssemblyDefinitionFile(asmdefPath, finalModuleName);
            }

            AddNamespaceExceptions(directoryConfigMap[selectedModuleType], modulePath);

            AssetDatabase.Refresh();
            new ModuleIndexRegistrar().Register(
                modulePath,
                directoryConfigMap[selectedModuleType],
                codeGenSettings.DirectoryStructureConfigMap.Keys
            );

            if (selectedModuleType == ModuleType.Screen)
            {
                HandleScreenModuleCreation(
                    moduleName,
                    modulePath,
                    testModulesFolderName,
                    selectedOptionalFolders,
                    directoryConfigMap,
                    codeGenSettings,
                    actionNames,
                    createScreen,
                    screenConfigData
                );
            }
            else
            {
                HandleStandardModuleCreation(
                    moduleName,
                    modulePath,
                    selectedModuleType,
                    directoryConfigMap,
                    createRoot,
                    createContext,
                    createSignals,
                    createScreen
                );
            }

            if (selectedModuleType != ModuleType.Test)
            {
                RegisterModuleLogType(moduleName);
            }
        }

        private static void RegisterModuleLogType(string moduleName)
        {
            var newTypes = new List<(string Name, int Value, Color LogColor)>
            {
                ($"{moduleName}Module", -1, Color.white)
            };
            FlowLogTypeManager.AddFlowLogTypesBatch(newTypes);
        }

        private static void ClearPrefs()
        {
            EditorPrefs.DeleteKey(KEY_FILE_NAME);
            EditorPrefs.DeleteKey(KEY_ROOT_NAME);
            EditorPrefs.DeleteKey(KEY_PARENT_FOLDER_PATH);
            EditorPrefs.DeleteKey(KEY_VIEW_NAMESPACE);
            EditorPrefs.DeleteKey(KEY_CONTEXT_NAMESPACE);
            EditorPrefs.DeleteKey(KEY_SCREEN_NAME);
            EditorPrefs.DeleteKey(KEY_SCENE_PATH);
            EditorPrefs.DeleteKey(SCREEN_PREFAB_PATH);
            EditorPrefs.DeleteKey(BOOL_CREATE_SCREEN);
            EditorPrefs.DeleteKey(KEY_MODULE_NAME);
            EditorPrefs.DeleteKey(MODULE_GENERATION_WORKING);
        }
    }
}
#endif