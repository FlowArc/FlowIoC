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
                .FindFullFolderPathByID(FolderConfig.FolderType.RootsAndContexts, modulePath);

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
                string signalsPath = directoryConfigMap[selectedModuleType]
                    .FindFullFolderPathByID(FolderConfig.FolderType.Signals, modulePath);

                if (!string.IsNullOrEmpty(signalsPath))
                {
                    string signalsName = CreateSignals(signalsPath, modulePath, moduleName, out string signalsNamespace);

                    if (createContext && !string.IsNullOrEmpty(rootsAndContextsPath))
                    {
                        CodeGeneratorUtils.BindSignalsInContext(
                            rootsAndContextsPath + "/" + moduleName + "Context.cs", signalsName, signalsNamespace);
                    }
                }
                else
                {
                    Debug.LogWarning(SIGNALS_WARNING);
                }
            }

            if (createScreen)
            {
                string scenePath = directoryConfigMap[selectedModuleType]
                    .FindFullFolderPathByID(FolderConfig.FolderType.Scenes, modulePath);
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
        /// Writes the module's signal holder and hands back the class name and the namespace it
        /// landed in. The namespace segment is read off the folder the config resolved rather than
        /// hardcoded, because the Signals folder can be renamed from the code generator settings
        /// like any other tracked folder.
        /// </summary>
        private static string CreateSignals(string path, string modulePath, string moduleName, out string signalsNamespace)
        {
            string signalsName = moduleName + "Signals";

            string moduleNamespace = NamespaceUtility.GetModuleNamespace(modulePath);
            signalsNamespace = $"{moduleNamespace}.{Path.GetFileName(path)}";

            CodeGeneratorUtils.CreateSignals(
                signalsName,
                "TempSignals",
                path,
                CodeGeneratorStrings.TempSignalsPath,
                signalsNamespace
            );

            return signalsName;
        }

        internal static void CreateFoldersRecursively(string basePath, List<FolderConfig> folders, List<FolderConfig> selectedOptionalFolders)
        {
            foreach (FolderConfig folder in folders)
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