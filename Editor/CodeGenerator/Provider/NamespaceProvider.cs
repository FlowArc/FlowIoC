#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using FlowIoC.Editor.CodeGenerator.Menus.Module;
using FlowIoC.Editor.CodeStyle;
using FlowIoC.Editor.Config.ModuleConfig;
using FlowIoC.Editor.Modules;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Provider
{
    internal static class NamespaceProvider
    {
        private const string MODULES_PATH = "Modules";

        public static void UpdateNamespaceSettings()
        {
            ModuleRegistry registry = new ModuleRegistryFactory().FromProject();
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

            ModuleFolders folders = new ModuleFolderPaths().Resolve(registry);

            foreach (SkippedModule skipped in folders.Skipped)
            {
                Debug.LogWarning($"[NamespaceProvider] Skipping '{skipped.Name}': {skipped.Reason}. "
                                 + "Rebuild the module index if that is unexpected.");
            }

            foreach (string modulePath in folders.Paths)
            {
                UpdateModuleDotSettings(registry, modulePath, projectRoot);
            }

            RunGuarded("orphan cleanup", () => CleanupOrphanedFiles(projectRoot));
            RunGuarded("solution code style", () => UpdateSolutionCodeStyle(projectRoot));

            AssetDatabase.Refresh();
            Debug.Log("All module-based .DotSettings files updated successfully.");
        }

        /// <summary>
        /// One module's settings file. A module that fails is reported and stepped over: the run
        /// also clears orphaned files and writes the solution code style, and none of that is
        /// worth losing over a single module the index is wrong about.
        /// </summary>
        private static void UpdateModuleDotSettings(ModuleRegistry registry, string modulePath, string projectRoot)
        {
            string moduleFolderName = Path.GetFileName(modulePath);

            try
            {
                string[] asmdefFiles = Directory.GetFiles(modulePath, "*.asmdef", SearchOption.TopDirectoryOnly);
                if (asmdefFiles.Length == 0)
                {
                    return;
                }

                string asmdefFileName = Path.GetFileNameWithoutExtension(asmdefFiles[0]);
                string dotSettingsFileName = asmdefFileName + ".csproj.DotSettings";
                string finalDotSettingsPath = Path.Combine(projectRoot, dotSettingsFileName);

                // Always recreate with correct Rider-native format
                NamespaceUtility.CreateDotSettingsFile(finalDotSettingsPath);

                XmlDocument doc = new XmlDocument();
                doc.Load(finalDotSettingsPath);

                AddNamespaceEntriesForModule_New(registry, doc, modulePath);

                NamespaceUtility.SaveDotSettings(doc, finalDotSettingsPath);
                Debug.Log($"[{moduleFolderName}] => .DotSettings updated: {finalDotSettingsPath}");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[NamespaceProvider] '{moduleFolderName}' was skipped: {exception.Message}");
            }
        }

        /// <summary>
        /// The two steps that close a run are independent of each other and of the modules, so
        /// neither is allowed to stop the other from happening.
        /// </summary>
        private static void RunGuarded(string what, Action step)
        {
            try
            {
                step();
            }
            catch (Exception exception)
            {
                Debug.LogError($"[NamespaceProvider] The {what} step failed: {exception.Message}");
            }
        }

        private static void UpdateSolutionCodeStyle(string projectRoot)
        {
            var writer = new SolutionDotSettingsWriter(projectRoot, new PackageCodeStyleTemplate().Resolve());

            foreach (string removed in writer.CleanupOrphaned())
            {
                Debug.Log($"[Cleanup] Orphaned solution DotSettings deleted: {Path.GetFileName(removed)}");
            }

            if (writer.TryWrite(out string path, out string error))
            {
                Debug.Log($"Solution code style updated: {path}");
                return;
            }

            Debug.LogError(error);
        }

        private static void CleanupOrphanedFiles(string projectRoot)
        {
            HashSet<string> existingAssemblyNames = CollectAllAsmdefNames(projectRoot);
            int cleanedCount = 0;

            string[] dotSettingsFiles = Directory.GetFiles(projectRoot, "*.csproj.DotSettings", SearchOption.TopDirectoryOnly);
            foreach (string filePath in dotSettingsFiles)
            {
                if (!filePath.EndsWith(".csproj.DotSettings", StringComparison.OrdinalIgnoreCase)) continue;

                string assemblyName = Path.GetFileName(filePath).Replace(".csproj.DotSettings", "");
                if (!existingAssemblyNames.Contains(assemblyName))
                {
                    File.Delete(filePath);
                    Debug.Log($"[Cleanup] Orphaned DotSettings deleted: {Path.GetFileName(filePath)}");
                    cleanedCount++;
                }
            }

            string[] csprojFiles = Directory.GetFiles(projectRoot, "Modules.*.csproj", SearchOption.TopDirectoryOnly);
            foreach (string filePath in csprojFiles)
            {
                if (!filePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)) continue;

                string assemblyName = Path.GetFileNameWithoutExtension(filePath);
                if (!existingAssemblyNames.Contains(assemblyName))
                {
                    File.Delete(filePath);
                    Debug.Log($"[Cleanup] Orphaned csproj deleted: {Path.GetFileName(filePath)}");
                    cleanedCount++;
                }
            }

            if (cleanedCount > 0)
                Debug.Log($"[Cleanup] Total orphaned files deleted: {cleanedCount}");
        }

        private static HashSet<string> CollectAllAsmdefNames(string projectRoot)
        {
            HashSet<string> names = new HashSet<string>();
            string[] searchPaths =
            {
                Path.Combine(projectRoot, "Assets"),
                Path.Combine(projectRoot, "Packages")
            };

            foreach (string searchPath in searchPaths)
            {
                if (!Directory.Exists(searchPath)) continue;

                string[] asmdefFiles = Directory.GetFiles(searchPath, "*.asmdef", SearchOption.AllDirectories);
                foreach (string asmdefPath in asmdefFiles)
                {
                    names.Add(Path.GetFileNameWithoutExtension(asmdefPath));
                }
            }

            return names;
        }

        private static void AddNamespaceEntriesForModule_New(ModuleRegistry registry, XmlDocument doc, string modulePath)
        {
            string moduleAssetPath = new ModuleAssetPathResolver().ToAssetPath(modulePath);
            if (!registry.TryGetModule(moduleAssetPath, out ModuleDescriptor module))
                return;

            DirectoryStructureConfig config = new DirectoryStructureConfigProvider().ConfigFor(module.Kind);
            if (config == null)
            {
                Debug.LogError($"[NEW] Could not find directory structure config for module kind {module.Kind}");
                return;
            }

            List<string> nonNamespaceFolders = new List<string>();
            CollectNonNamespaceFolders(modulePath, config.RootFolders, nonNamespaceFolders);

            foreach (string folderPath in nonNamespaceFolders)
            {
                NamespaceUtility.AddNamespaceFolderToSkip(doc, folderPath);
            }

            AddAncestorSkipFolders(modulePath, doc);
        }

        private static void AddAncestorSkipFolders(string modulePath, XmlDocument doc)
        {
            string modulesRoot = Path.Combine(Application.dataPath, MODULES_PATH).Replace('\\', '/');

            DirectoryInfo current = Directory.GetParent(modulePath);
            while (current != null)
            {
                string fullName = current.FullName.Replace('\\', '/');
                if (fullName.Length <= modulesRoot.Length ||
                    !fullName.StartsWith(modulesRoot, StringComparison.OrdinalIgnoreCase))
                    break;

                if (NamespaceUtility.SkipFolderNames.Contains(current.Name, StringComparer.Ordinal))
                {
                    NamespaceUtility.AddNamespaceFolderToSkip(doc, current.FullName);
                }

                current = current.Parent;
            }
        }

        private static void CollectNonNamespaceFolders(string basePath, List<FolderConfig> folders, List<string> nonNamespaceFolders)
        {
            foreach (FolderConfig folder in folders)
            {
                if (!folder.IsMandatory && !folder.IsOptional)
                    continue;

                string folderPath = Path.Combine(basePath, folder.FolderName);

                if (!folder.IsNamespaceProvider)
                {
                    nonNamespaceFolders.Add(folderPath);
                }

                if (folder.SubFolders != null && folder.SubFolders.Count > 0)
                {
                    CollectNonNamespaceFolders(folderPath, folder.SubFolders, nonNamespaceFolders);
                }
            }
        }
    }
}
#endif