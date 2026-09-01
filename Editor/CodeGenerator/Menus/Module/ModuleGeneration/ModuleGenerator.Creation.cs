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
            bool createScreen
        )
        {
            string rootsAndContextsPath = directoryConfigMap[selectedModuleType]
                .FindFullFolderPathByID(FolderEVO.FolderType.RootsAndContexts, modulePath);

            if (!string.IsNullOrEmpty(rootsAndContextsPath))
            {
                if (createRoot)
                {
                    CreateRoot(rootsAndContextsPath, modulePath, moduleName, selectedModuleType == ModuleType.Test);
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

            if (createSignals)
            {
                WriteSignalHolders(
                    moduleName, modulePath, selectedModuleType, directoryConfigMap, createContext, rootsAndContextsPath);
            }

            if (createScreen)
            {
                string scenePath = directoryConfigMap[selectedModuleType]
                    .FindFullFolderPathByID(FolderEVO.FolderType.Scenes, modulePath);
                CreateScene(scenePath, moduleName);
                EditorPrefs.SetBool(BOOL_CREATE_SCREEN, true);
            }
        }

        private static void CreateRoot(string path, string modulePath, string moduleName, bool isTest)
        {
            string suffix = isTest ? "" : "";
            string rootName = moduleName + suffix + "Root";
            string contextName = moduleName + suffix + "Context";

            string moduleNamespace = NamespaceUtility.GetModuleNamespace(modulePath);
            string rootsAndContextsNamespace = $"{moduleNamespace}.RootsContexts";

            CodeGeneratorUtils.CreateRoot(
                rootName,
                contextName,
                "TempContext",
                "TempRoot",
                path,
                CodeGeneratorStrings.TempRootPath,
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

        /// <summary>
        /// Writes the module's two signal holders and binds whichever of them landed into the
        /// Context.
        ///
        /// The public holder goes into Scripts/Shared/Signals so it compiles into the module's
        /// Shared assembly: a Connector reaches a module's signals through Modules.X.Shared and
        /// never through the assembly holding its Models and Commands. A module created without
        /// Shared has no such folder, and the holder falls back to Scripts/Runtime/Signals so the
        /// module still works - it just cannot be wired to from outside without a direct reference.
        ///
        /// The internal holder always goes into Scripts/Runtime/Signals, because it is the module
        /// talking to its own commands and nothing outside the module may dispatch it.
        /// </summary>
        private static void WriteSignalHolders(
            string moduleName,
            string modulePath,
            ModuleType selectedModuleType,
            Dictionary<ModuleType, DirectoryStructureConfig> directoryConfigMap,
            bool createContext,
            string rootsAndContextsPath)
        {
            bool isTest = selectedModuleType == ModuleType.Test;
            DirectoryStructureConfig config = directoryConfigMap[selectedModuleType];

            string signalsPath = config.FindFullFolderPathByID(FolderEVO.FolderType.Signals, modulePath);
            string sharedSignalsPath = config.FindFullFolderPathByID(FolderEVO.FolderType.SharedSignals, modulePath);

            if (!string.IsNullOrEmpty(sharedSignalsPath) && !Directory.Exists(sharedSignalsPath))
                sharedSignalsPath = null;

            string publicSignalsPath = string.IsNullOrEmpty(sharedSignalsPath) ? signalsPath : sharedSignalsPath;

            if (string.IsNullOrEmpty(publicSignalsPath))
            {
                Debug.LogWarning(SIGNALS_WARNING);
                return;
            }

            string contextPath = rootsAndContextsPath + "/" + moduleName + "Context.cs";
            bool bindInContext = createContext && !string.IsNullOrEmpty(rootsAndContextsPath);

            string signalsName = CreateSignals(publicSignalsPath, moduleName + "Signals", "TempSignals",
                CodeGeneratorStrings.TempSignalsPath, isTest, true, out string signalsNamespace);

            if (bindInContext)
                CodeGeneratorUtils.BindSignalsInContext(contextPath, signalsName, signalsNamespace);

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
            // the public holder sits two namespace providers deep, under Shared and then Signals,
            // and this is the lookup that already knows which folders provide a namespace at all.
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