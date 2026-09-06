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

            string finalAssemblyName = GetParsedAssemblyName(rawFolderName);

            string assemblyFileName = finalAssemblyName + ".asmdef";
            string assemblyFilePath = Path.Combine(modulePath, assemblyFileName);

            if (!File.Exists(assemblyFilePath))
                return;

            WriteNamespaceExceptions(config, modulePath, finalAssemblyName);
        }

        /// <summary>
        /// The same entries again, written under the name of one of the assemblies the module
        /// carves out of itself - its Shared or its Signals.
        ///
        /// A .csproj.DotSettings only applies to the project it is named after, and each of those
        /// folders is a project of its own - so the module's own file cannot tell Rider to skip
        /// the Scripts folder on their behalf. Without this a shared value object would land in
        /// Modules.PlayerModule.Scripts.Shared.Data.ValueObjects, carrying the Scripts folder in
        /// the middle of its namespace.
        ///
        /// A null or empty name means the module has no such assembly, which is an ordinary
        /// answer: Shared is optional, and a test module has neither.
        /// </summary>
        internal static void AddSubAssemblyNamespaceExceptions(
            DirectoryStructureConfig config, string modulePath, string subAssemblyName)
        {
            if (string.IsNullOrEmpty(subAssemblyName))
                return;

            WriteNamespaceExceptions(config, modulePath, subAssemblyName);
        }

        /// <summary>
        /// Every project skips the same folders: the ones the config marks as no namespace
        /// provider, and the z folders the module hangs under. A Shared or Signals project holds
        /// no file under Runtime or the z folders, so the entries it has no use for cost it
        /// nothing, and writing the same set keeps the files reading alike.
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

            ED_CodeGenerator codeGenSettings = AssetDatabase.LoadAssetAtPath<ED_CodeGenerator>(CodeGeneratorStrings.CONFIG_PATH);
            if (codeGenSettings == null)
            {
                Debug.LogError($"ED_CodeGenerator asset not found. Please ensure it exists at {CodeGeneratorStrings.CONFIG_PATH}.");
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

                if (segment.Equals(codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.SubModules]) ||
                    segment.Equals(codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.ScreenModules]) ||
                    segment.Equals(codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.TestModules]))
                {
                    NamespaceUtility.SetNamespaceProvider(currentPath, false, dotSettingsFilePath);
                }
            }
        }

        private static void TraverseFoldersForNamespaceExceptions(
            List<FolderEVO> folders,
            string basePath,
            string dotSettingsFilePath
        )
        {
            foreach (FolderEVO folder in folders)
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