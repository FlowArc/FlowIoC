#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using FlowIoC.ConsoleModule;
using FlowIoC.Editor.CodeGenerator.Menus.Module.CreateModule;
using FlowIoC.Editor.CodeGenerator.Screens;
using FlowIoC.Editor.Config.ModuleConfig;
using FlowIoC.Editor.ModuleCards;
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
        private const string KEY_SCREEN_CONTEXT_FULL_NAME = "screen-context-full-name";

        private static ModuleType _selectedModuleType;

        public static void CreateModuleStructure(
            string moduleName,
            string parentModulePath,
            ModuleType selectedModuleType,
            List<FolderEVO> selectedOptionalFolders,
            Dictionary<ModuleType, DirectoryStructureConfig> directoryConfigMap,
            List<string> actionNames,
            bool createRoot,
            bool createContext,
            bool createSignals,
            bool createScreen,
            bool allowAsSubContext,
            ModuleRole moduleRole,
            ScreenModuleSettings screenSettings = null,
            ModuleCardDraftEVO card = null
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

            ED_CodeGenerator codeGenSettings = AssetDatabase.LoadAssetAtPath<ED_CodeGenerator>(CodeGeneratorStrings.CONFIG_PATH);
            if (codeGenSettings == null)
            {
                Debug.LogError($"ED_CodeGenerator asset not found. Please ensure it exists at {CodeGeneratorStrings.CONFIG_PATH}.");
                return;
            }

            string subModulesFolderName = codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.SubModules];
            string testModulesFolderName = codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.TestModules];
            string screenModulesFolderName = codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.ScreenModules];

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
                parentModulePath,
                selectedModuleType,
                selectedOptionalFolders,
                directoryConfigMap,
                codeGenSettings,
                actionNames,
                createRoot,
                createContext,
                createSignals,
                createScreen,
                allowAsSubContext,
                moduleRole,
                screenSettings,
                testModulesFolderName,
                card
            );
        }

        private static void CreateAndUpdateModules(
            string moduleName,
            string modulePath,
            string parentModulePath,
            ModuleType selectedModuleType,
            List<FolderEVO> selectedOptionalFolders,
            Dictionary<ModuleType, DirectoryStructureConfig> directoryConfigMap,
            ED_CodeGenerator codeGenSettings,
            List<string> actionNames,
            bool createRoot,
            bool createContext,
            bool createSignals,
            bool createScreen,
            bool allowAsSubContext,
            ModuleRole moduleRole,
            ScreenModuleSettings screenSettings,
            string testModulesFolderName,
            ModuleCardDraftEVO card
        )
        {
            // The module this one lives in, if any. A top level module is parented to
            // Assets/Modules, which has no Shared folder, so the lookup simply finds nothing there
            // and no special case is needed for it.
            string parentSharedAssemblyName =
                new SharedAssemblyDefinition().FindIn(parentModulePath, directoryConfigMap[ModuleType.Main]);

            string sharedAssemblyName = null;
            string signalsAssemblyName = null;

            if (selectedModuleType == ModuleType.Main || selectedModuleType == ModuleType.Test)
            {
                string finalModuleName = moduleName + "Module";
                string asmdefPath = Path.Combine(modulePath, finalModuleName + ".asmdef");

                // The Shared assembly has to exist before the module references it, and the module
                // has to reference it at all: the asmdef inside Scripts/Shared takes that folder
                // out of the module's own assembly, so without this a module could not read the
                // data it publishes.
                sharedAssemblyName = new SharedAssemblyDefinition()
                    .CreateFor(modulePath, directoryConfigMap[selectedModuleType], GetParsedAssemblyName(finalModuleName));

                // The Signals assembly, for the same reason and with its own twist: the holder may
                // be generic over a type the module publishes, so this assembly references the
                // module's own Shared. A holder generic over another module's published type needs
                // that module's Shared added by hand - which survives, because nothing here ever
                // rewrites a reference list it did not write.
                signalsAssemblyName = new SignalsAssemblyDefinition()
                    .CreateFor(modulePath, directoryConfigMap[selectedModuleType], GetParsedAssemblyName(finalModuleName),
                        sharedAssemblyName);

                // A test module exists to exercise the module it sits under, and is allowed to
                // reach anything, so it is wired to its parent outright - the parent's own
                // assembly, its Shared and its Signals - rather than only to what that parent
                // publishes. Every other module type gets its parent's Shared and nothing more:
                // reaching a neighbour's Models, Commands or signals is the one thing the
                // architecture does not allow.
                string parentAssemblyName = null;
                string parentSignalsAssemblyName = null;

                if (selectedModuleType == ModuleType.Test)
                {
                    parentAssemblyName = ParentModuleAssemblyName(parentModulePath);
                    parentSignalsAssemblyName =
                        new SignalsAssemblyDefinition().FindIn(parentModulePath, directoryConfigMap[ModuleType.Main]);
                }

                CreateAssemblyDefinitionFile(
                    asmdefPath, finalModuleName, sharedAssemblyName, signalsAssemblyName, parentSharedAssemblyName,
                    parentSignalsAssemblyName, parentAssemblyName);
            }

            AddNamespaceExceptions(directoryConfigMap[selectedModuleType], modulePath);
            AddSubAssemblyNamespaceExceptions(directoryConfigMap[selectedModuleType], modulePath, sharedAssemblyName);
            AddSubAssemblyNamespaceExceptions(directoryConfigMap[selectedModuleType], modulePath, signalsAssemblyName);

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
                    parentModulePath,
                    testModulesFolderName,
                    selectedOptionalFolders,
                    directoryConfigMap,
                    codeGenSettings,
                    actionNames,
                    createScreen,
                    screenSettings,
                    parentSharedAssemblyName
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
                    createScreen,
                    allowAsSubContext,
                    moduleRole
                );
            }

            if (selectedModuleType != ModuleType.Test)
            {
                RegisterModuleLogType(moduleName);
                WriteModuleCard(moduleName, modulePath, card);
            }
        }

        /// <summary>
        /// The module's card, written for both kinds of module rather than inside one of the two
        /// branches above - a screen module owes one as much as any other.
        ///
        /// The authored half only. Its generated block names assemblies that do not exist until
        /// this module compiles, so Module Scanner writes that on the next scan, and reports the
        /// placeholders above it until somebody says what the module is for.
        /// </summary>
        private static void WriteModuleCard(string moduleName, string modulePath, ModuleCardDraftEVO card)
        {
            new ModuleCardFile().Write(modulePath, new ModuleCardStub().For(moduleName, card));
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
            EditorPrefs.DeleteKey(KEY_SCREEN_CONTEXT_FULL_NAME);
            EditorPrefs.DeleteKey(MODULE_GENERATION_WORKING);
        }
    }
}
#endif