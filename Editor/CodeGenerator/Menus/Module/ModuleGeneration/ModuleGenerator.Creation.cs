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
            bool createSignals,
            bool createScreen,
            bool allowAsSubContext,
            ModuleRole moduleRole
        )
        {
            string rootsAndContextsPath = directoryConfigMap[selectedModuleType]
                .FindFullFolderPathByID(FolderEVO.FolderType.RootsAndContexts, modulePath);

            if (!string.IsNullOrEmpty(rootsAndContextsPath))
            {
                if (createRoot)
                {
                    // Only a main module is ever the project's frame. Every other kind arrives here
                    // carrying Core as "no role at all", which is why the type is asked as well as
                    // the role - a test module's Root must read as a Test, not as the frame.
                    CreateRoot(rootsAndContextsPath, modulePath, moduleName, moduleRole,
                        selectedModuleType == ModuleType.Test,
                        moduleRole == ModuleRole.Core && selectedModuleType == ModuleType.Main);
                }

                if (createContext)
                {
                    // Only a module that gets a Root of its own is ever kept out of Add Sub
                    // Context, so only that module is written with the attribute that puts it
                    // back. A screen module's context never reaches here, and a test module's is
                    // not offered either way.
                    CreateContext(rootsAndContextsPath, modulePath, moduleName, moduleRole,
                        selectedModuleType == ModuleType.Test,
                        allowAsSubContext && createRoot && selectedModuleType == ModuleType.Main);
                }
            }
            else
            {
                Debug.LogWarning(ROOTS_CONTEXTS_WARNING);
            }

            if (createSignals)
            {
                WriteSignalHolders(
                    moduleName, modulePath, selectedModuleType, moduleRole, directoryConfigMap, createContext,
                    rootsAndContextsPath);
            }

            if (createScreen)
            {
                string scenePath = directoryConfigMap[selectedModuleType]
                    .FindFullFolderPathByID(FolderEVO.FolderType.Scenes, modulePath);
                CreateScene(scenePath, moduleName);
                EditorPrefs.SetBool(BOOL_CREATE_SCREEN, true);
            }
        }

        /// <summary>
        /// The module's Root, named for what it roots. A System module's Root is PlayerSystemRoot
        /// and a Service module's is CounterServiceRoot, which is what the Root inspector reads to
        /// paint it; a Core module keeps the plain PlayerRoot. The name is left in EditorPrefs
        /// because the scene the generator builds after the reload has to find the type again.
        /// </summary>
        private static void CreateRoot(string path, string modulePath, string moduleName, ModuleRole moduleRole,
            bool isTest, bool isCore)
        {
            var naming = new ModuleRoleNaming();
            string rootName = naming.RootName(moduleName, moduleRole);
            string contextName = naming.ContextName(moduleName, moduleRole);

            EditorPrefs.SetString(KEY_ROOT_NAME, rootName);

            string moduleNamespace = NamespaceUtility.GetModuleNamespace(modulePath);
            string rootsAndContextsNamespace = $"{moduleNamespace}.RootsContexts";

            // A System and a Service say what they root in the Root's own name, which is what the
            // inspector reads. Core carries no suffix - there is one Main and one Screen in a
            // project, so the name has nothing to disambiguate - and the attribute is what tells
            // the bar that this Root is the project's frame.
            string attribute = isCore ? "[FlowHeader(FlowRole.Core)]" : null;

            CodeGeneratorUtils.CreateRoot(
                rootName,
                contextName,
                "TempContext",
                "TempRoot",
                path,
                CodeGeneratorStrings.TempRootPath,
                rootsAndContextsNamespace,
                isTest,
                attribute
            );
        }

        private static void CreateContext(string path, string modulePath, string moduleName, ModuleRole moduleRole,
            bool isTest, bool allowAsSubContext)
        {
            string contextName = new ModuleRoleNaming().ContextName(moduleName, moduleRole);

            string moduleNamespace = NamespaceUtility.GetModuleNamespace(modulePath);
            string rootsAndContextsNamespace = $"{moduleNamespace}.RootsContexts";

            CodeGeneratorUtils.CreateContext(
                contextName,
                "TempContext",
                path,
                CodeGeneratorStrings.TempContextPath,
                rootsAndContextsNamespace,
                false,
                isTest,
                allowAsSubContext
            );
            EditorPrefs.SetString(KEY_CONTEXT_NAMESPACE, rootsAndContextsNamespace);
        }

        /// <summary>
        /// Writes the module's two signal holders and binds whichever of them landed into the
        /// Context.
        ///
        /// The public holder goes into Scripts/Signals so it compiles into the module's Signals
        /// assembly: a Connector reaches a module's signals through Modules.X.Signals, and neither
        /// the assembly holding its Models and Commands nor the Shared assembly holding its
        /// published data will do. A module created without that folder gets no public holder at
        /// all - that is what leaving the folder out means. It used to fall back to
        /// Scripts/Runtime/Signals, which was invisible while the folder was mandatory and would
        /// now quietly hand a module a public holder nothing outside it can reach.
        ///
        /// The internal holder always goes into Scripts/Runtime/Signals, because it is the module
        /// talking to its own commands and nothing outside the module may dispatch it. The two
        /// share a namespace and differ in assembly, which is the whole distinction: one crosses a
        /// boundary and the other has none to cross.
        /// </summary>
        private static void WriteSignalHolders(
            string moduleName,
            string modulePath,
            ModuleType selectedModuleType,
            ModuleRole moduleRole,
            Dictionary<ModuleType, DirectoryStructureConfig> directoryConfigMap,
            bool createContext,
            string rootsAndContextsPath)
        {
            bool isTest = selectedModuleType == ModuleType.Test;
            DirectoryStructureConfig config = directoryConfigMap[selectedModuleType];

            string signalsPath = config.FindFullFolderPathByID(FolderEVO.FolderType.Signals, modulePath);
            string publicSignalsPath = config.FindFullFolderPathByID(FolderEVO.FolderType.PublicSignals, modulePath);

            if (!string.IsNullOrEmpty(publicSignalsPath) && !Directory.Exists(publicSignalsPath))
                publicSignalsPath = null;

            if (string.IsNullOrEmpty(publicSignalsPath) && string.IsNullOrEmpty(signalsPath))
            {
                Debug.LogWarning(SIGNALS_WARNING);
                return;
            }

            string contextName = new ModuleRoleNaming().ContextName(moduleName, moduleRole);
            string contextPath = rootsAndContextsPath + "/" + contextName + ".cs";
            bool bindInContext = createContext && !string.IsNullOrEmpty(rootsAndContextsPath);

            if (!string.IsNullOrEmpty(publicSignalsPath))
            {
                string signalsName = CreateSignals(publicSignalsPath, moduleName + "Signals", "TempSignals",
                    CodeGeneratorStrings.TempSignalsPath, isTest, true, out string signalsNamespace);

                if (bindInContext)
                    CodeGeneratorUtils.BindSignalsInContext(contextPath, signalsName, signalsNamespace);
            }

            if (string.IsNullOrEmpty(signalsPath)) return;

            string internalName = CreateSignals(signalsPath, moduleName + "InternalSignals", "TempInternalSignals",
                CodeGeneratorStrings.TempInternalSignalsPath, isTest, false, out string internalNamespace);

            if (bindInContext)
                CodeGeneratorUtils.BindSignalsInContext(contextPath, internalName, internalNamespace, "_internalSignals");
        }

        /// <summary>
        /// Writes one signal holder and hands back the class name and the namespace it landed in.
        /// The namespace segment is read off the folder the config resolved rather than hardcoded,
        /// because a Signals folder can be renamed from the code generator settings like any other
        /// tracked folder.
        /// </summary>
        private static string CreateSignals(string path, string signalsName, string tempClassName,
            string tempClassPath, bool isTest, bool makePublic, out string signalsNamespace)
        {
            // Read off the folder rather than assembled from the module namespace and one segment:
            // a Signals folder can be renamed from the code generator settings like any other
            // tracked folder, and this is the lookup that already knows which folders provide a
            // namespace at all.
            signalsNamespace = NamespaceUtility.GetFullNamespaceForFile(Path.Combine(path, signalsName + ".cs"));

            CodeGeneratorUtils.CreateSignals(
                signalsName,
                tempClassName,
                path,
                tempClassPath,
                signalsNamespace,
                isTest,
                makePublic
            );

            return signalsName;
        }

        internal static void CreateFoldersRecursively(string basePath, List<FolderEVO> folders, List<FolderEVO> selectedOptionalFolders)
        {
            foreach (FolderEVO folder in folders)
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
    }
}
#endif