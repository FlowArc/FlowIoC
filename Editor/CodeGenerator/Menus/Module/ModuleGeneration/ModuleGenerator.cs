#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlowIoC.Editor.Console;
using FlowIoC.Editor.CodeGenerator.Menus.Module.CreateModule;
using FlowIoC.Editor.CodeGenerator.Screens;
using FlowIoC.Editor.Config.ModuleConfig;
using FlowIoC.Editor.ModuleCards;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration
{
    /// <summary>
    /// A run has two halves. The first, below, writes the folders, the assemblies and the
    /// scripts, and makes the scene when one is asked for; the scripts trigger a domain reload.
    /// The second, in the PostProcess part, runs after that reload and puts the compiled Root
    /// into the scene. The two talk through one <see cref="ModuleGenerationHandoffEVO"/>,
    /// written whole at the end of the first half and consumed at the start of the second.
    /// </summary>
    internal partial class ModuleGenerator
    {
        private const string ROOTS_CONTEXTS_WARNING = "Roots&Contexts folder not found!";
        private const string SIGNALS_WARNING = "Signals folder not found!";
        private const string PARENT_MODULE_REQUIRED_TITLE = "Parent Module Required";
        private const string PARENT_MODULE_REQUIRED_MESSAGE = "Please select a parent module";

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
            if (string.IsNullOrEmpty(parentModulePath))
            {
                EditorUtility.DisplayDialog(PARENT_MODULE_REQUIRED_TITLE, PARENT_MODULE_REQUIRED_MESSAGE, "OK");
                return;
            }

            // The type's suffix goes on here, not in the window: "Ads" with type Test is
            // AdsTestModule whoever asked. Left to the window, a call from a script that named a
            // test module "Ads" wrote zTestModules/AdsModule, with the parent's own assembly name.
            moduleName = new ModuleTypeSuffix().Apply(moduleName, selectedModuleType);

            // The role for the same reason. A script that asked for a test module carrying System
            // got GameplayTestSystemRoot, where the window would have written GameplayTestRoot.
            moduleRole = new ModuleRoleNaming().Effective(moduleRole, selectedModuleType, createRoot);

            ED_CodeGenerator codeGenSettings = AssetDatabase.LoadAssetAtPath<ED_CodeGenerator>(CodeGeneratorStrings.CONFIG_PATH);
            if (codeGenSettings == null)
            {
                Debug.LogError($"ED_CodeGenerator asset not found. Please ensure it exists at {CodeGeneratorStrings.CONFIG_PATH}.");
                return;
            }

            // The scene is made beside the open ones and closed again, so what is open is never
            // closed and nobody is asked about it. Unity makes no new scene beside an untitled
            // one, though, so that run stops here, before the first folder is written - and says
            // so in the console rather than in a modal, which stops the Editor until somebody
            // answers it and leaves an agent driving the Editor waiting for ever.
            if (createScreen && new UntitledScene().IsOpen)
            {
                Debug.LogError("<color=cyan>[FlowIoC]</color> Create Module makes its scene beside the open scenes, and "
                               + "Unity makes no new scene beside an untitled one. Save the untitled scene or open a "
                               + "saved one, then create the module again. Nothing was written.");
                return;
            }

            string subModulesFolderName = codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.SubModules];
            string testModulesFolderName = codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.TestModules];
            string screenModulesFolderName = codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.ScreenModules];

            string resolvedModuleKind = selectedModuleType switch
            {
                ModuleType.Main => IsModulesFolder(parentModulePath) ? "Main" : "Sub",
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

            if (IsExistingModule(modulePath))
            {
                AddMissingFolders(moduleName, modulePath, directoryConfigMap[selectedModuleType], selectedOptionalFolders,
                    codeGenSettings);
                return;
            }

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

            var handoff = new ModuleGenerationHandoffEVO {ModuleType = selectedModuleType, ModuleName = moduleName};

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
                    parentSharedAssemblyName,
                    handoff
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
                    moduleRole,
                    handoff
                );
            }

            if (selectedModuleType != ModuleType.Test)
            {
                WriteModuleCard(moduleName, modulePath, card);

                // The index above already knows the module, so its channel's part is written now
                // rather than on the next load - the module's own code compiles against it.
                FlowModuleGenerator.Generate();
            }

            // Written last, and whole: the record is this run's answers and nothing else, and a
            // run that stopped before this line left nothing for the reload to act on.
            new ModuleGenerationHandoffStore().Write(handoff);
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

        /// <summary>
        /// Whether the parent is Assets/Modules itself, in whatever form the caller wrote it. The
        /// panel hands Path.Combine(Application.dataPath, "Modules"); a script is as likely to hand
        /// the same folder with its separators the other way round or in another casing, which a
        /// plain string comparison read as a sub module's parent - and wrote the module under
        /// zSubModules.
        /// </summary>
        private static bool IsModulesFolder(string path) =>
            string.Equals(FullPathOf(path), FullPathOf(Path.Combine(Application.dataPath, "Modules")),
                StringComparison.OrdinalIgnoreCase);

        private static string FullPathOf(string path) => Path.GetFullPath(path).Replace('\\', '/').TrimEnd('/');

        /// <summary>
        /// A module is there once its folder holds its assembly or its card. A folder with neither -
        /// made by hand ahead of the run - is a module still to be created, and gets the whole run.
        /// </summary>
        private static bool IsExistingModule(string modulePath) =>
            Directory.Exists(modulePath)
            && (Directory.GetFiles(modulePath, "*.asmdef", SearchOption.TopDirectoryOnly).Length > 0
                || new ModuleCardFile().Exists(modulePath));

        /// <summary>
        /// Create Module run again on a module that exists, which is how a module gets a folder it
        /// was created without. Only the missing folders are made, and no file is written: the
        /// asmdefs, the card, the Root, the Context and the signal holders are the module's own by
        /// now, and a second run used to write each of them again from its template - the card back
        /// to placeholders, the asmdef back to the references it started with, and the public holder
        /// back to empty.
        ///
        /// Scripts/Shared and Scripts/Signals are not made here when the module lacks them. Each is
        /// an assembly of its own that the module has to reference and a holder or data has to fill,
        /// and Add Shared or Signals is the tool that does all of it; a folder alone would compile
        /// into the module's own assembly. The optional folders under one the module already has are
        /// made like any other.
        /// </summary>
        private static void AddMissingFolders(string moduleName, string modulePath, DirectoryStructureConfig config,
            List<FolderEVO> selectedOptionalFolders, ED_CodeGenerator codeGenSettings)
        {
            var before = new HashSet<string>(Directory.GetDirectories(modulePath, "*", SearchOption.AllDirectories));

            List<FolderEVO> folders = selectedOptionalFolders
                .Where(folder => !IsAssemblyFolder(folder)
                                 || Directory.Exists(config.FindFullFolderPathByID(folder.Type, modulePath)))
                .ToList();
            List<FolderEVO> withheld = selectedOptionalFolders.Except(folders).ToList();

            CreateFoldersRecursively(modulePath, config.RootFolders, folders);
            AddNamespaceExceptions(config, modulePath);

            AssetDatabase.Refresh();
            new ModuleIndexRegistrar().Register(modulePath, config, codeGenSettings.DirectoryStructureConfigMap.Keys);

            List<string> added = Directory.GetDirectories(modulePath, "*", SearchOption.AllDirectories)
                .Where(folder => !before.Contains(folder))
                .Select(folder => Path.GetRelativePath(modulePath, folder).Replace('\\', '/'))
                .ToList();

            string report = added.Count == 0
                ? $"<color=cyan>[FlowIoC]</color> {moduleName}Module already exists and has every folder asked for. No file in it was written."
                : $"<color=cyan>[FlowIoC]</color> {moduleName}Module already exists, so only its missing folders were made: "
                  + $"{string.Join(", ", added)}. No file in it was written.";

            if (withheld.Count > 0)
                report += $" Scripts/{string.Join(" and Scripts/", withheld.Select(folder => folder.FolderName))} "
                          + $"{(withheld.Count == 1 ? "comes" : "come")} from Tools/FlowIoC/Edit Module/Add Shared or "
                          + "Signals, which writes the assembly and its references with the folder.";

            Debug.Log(report);
        }

        private static bool IsAssemblyFolder(FolderEVO folder) =>
            folder.Type == FolderEVO.FolderType.Shared || folder.Type == FolderEVO.FolderType.PublicSignals;
    }
}
#endif