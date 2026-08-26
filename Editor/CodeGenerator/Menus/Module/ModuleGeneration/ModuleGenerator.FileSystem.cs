#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlowIoC.Editor.Config.ModuleConfig;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration
{
    internal partial class ModuleGenerator
    {
        private static void EnsureNamespaceImport(string className, string path, string type)
        {
            string filePath = path + "/" + className + ".cs";
            string[] fileLines = File.ReadAllLines(filePath);

            string moduleNamespace = NamespaceUtility.GetModuleNamespace(Path.GetDirectoryName(filePath));
            string namespaceLine = $"using {moduleNamespace}.{type};";

            if (fileLines.Any(line => line.Contains(namespaceLine))) return;

            List<string> newLines = new List<string>(fileLines);
            newLines.Insert(1, namespaceLine);
            File.WriteAllLines(filePath, newLines);
            AssetDatabase.Refresh();
        }

        private static void AddNamespaceExceptions(DirectoryStructureConfig config, string modulePath)
        {
            string rawFolderName = Path.GetFileName(modulePath);

            string finalAssemblyName = ConvertToModuleAssemblyName(rawFolderName);

            string assemblyFileName = finalAssemblyName + ".asmdef";
            string assemblyFilePath = Path.Combine(modulePath, assemblyFileName);

            if (!File.Exists(assemblyFilePath))
                return;

            WriteNamespaceExceptions(config, modulePath, finalAssemblyName);
        }

        /// <summary>
        /// The same entries again, written under the Shared assembly's name.
        ///
        /// A .csproj.DotSettings only applies to the project it is named after, and Scripts/Shared
        /// is a project of its own - so the module's own file cannot tell Rider to skip the Scripts
        /// folder on the Shared assembly's behalf. Without this a shared value object would land in
        /// Modules.PlayerModule.Scripts.Shared.Data.ValueObjects, carrying the Scripts folder in
        /// the middle of its namespace.
        /// </summary>
        internal static void AddSharedNamespaceExceptions(DirectoryStructureConfig config, string modulePath, string sharedAssemblyName)
        {
            if (string.IsNullOrEmpty(sharedAssemblyName))
                return;

            WriteNamespaceExceptions(config, modulePath, sharedAssemblyName);
        }

        /// <summary>
        /// Both projects skip the same folders: the ones the config marks as no namespace provider,
        /// and the z folders the module hangs under. The Shared project holds no file under Runtime
        /// or the z folders, so the entries it has no use for cost it nothing, and writing the same
        /// set keeps the two files reading alike.
        /// </summary>
        private static void WriteNamespaceExceptions(DirectoryStructureConfig config, string modulePath, string assemblyName)
        {
            string dotSettingsFileName = assemblyName + ".csproj.DotSettings";
            string dotSettingsFilePath = Path.Combine(modulePath, dotSettingsFileName);

            if (!File.Exists(dotSettingsFilePath))
            {
                NamespaceUtility.CreateDotSettingsFile(dotSettingsFilePath);
            }

            TraverseFoldersForNamespaceExceptions(config.RootFolders, modulePath, dotSettingsFilePath);

            AddParentPathNamespaceExceptions(modulePath, dotSettingsFilePath);
        }

        private static void AddParentPathNamespaceExceptions(string modulePath, string dotSettingsFilePath)
        {
            string assetsRelativePath = modulePath.Replace(Application.dataPath, "").TrimStart('/', '\\');
            string[] pathSegments = assetsRelativePath.Split(new[] {Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar},
                StringSplitOptions.RemoveEmptyEntries);

            if (pathSegments.Length <= 1)
                return;

            string currentPath = Path.Combine(Application.dataPath);

            CodeGeneratorSettings codeGenSettings = AssetDatabase.LoadAssetAtPath<CodeGeneratorSettings>(CodeGeneratorStrings.CONFIG_PATH);
            if (codeGenSettings == null)
            {
                Debug.LogError($"CodeGeneratorSettings asset not found. Please ensure it exists at {CodeGeneratorStrings.CONFIG_PATH}.");
                return;
            }

            for (int i = 0; i < pathSegments.Length; i++)
            {
                string segment = pathSegments[i];
                currentPath = Path.Combine(currentPath, segment);

                // if (segment.EndsWith("Module") && currentPath != modulePath)
                // {
                //     NamespaceUtility.SetNamespaceProvider(currentPath, false, dotSettingsFilePath);
                // }

                if (segment.Equals(codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.SubModules]) ||
                    segment.Equals(codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.ScreenModules]) ||
                    segment.Equals(codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.TestModules]))
                {
                    NamespaceUtility.SetNamespaceProvider(currentPath, false, dotSettingsFilePath);
                }
            }
        }

        private static void TraverseFoldersForNamespaceExceptions(
            List<FolderConfig> folders,
            string basePath,
            string dotSettingsFilePath
        )
        {
            foreach (FolderConfig folder in folders)
            {
                string folderPath = Path.Combine(basePath, folder.FolderName);

                if (!folder.IsNamespaceProvider)
                {
                    NamespaceUtility.SetNamespaceProvider(
                        folderPath,
                        false,
                        dotSettingsFilePath
                    );
                }
                else
                {
                    NamespaceUtility.SetNamespaceProvider(
                        folderPath,
                        true,
                        dotSettingsFilePath
                    );
                }

                if (folder.SubFolders != null && folder.SubFolders.Count > 0)
                {
                    TraverseFoldersForNamespaceExceptions(folder.SubFolders, folderPath, dotSettingsFilePath);
                }
            }
        }
    }
}
#endif