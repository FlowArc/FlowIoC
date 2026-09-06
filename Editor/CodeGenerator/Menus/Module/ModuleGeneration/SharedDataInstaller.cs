#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.Config.ModuleConfig;
using FlowIoC.Editor.Modules;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration
{
    /// <summary>
    /// What Create Module does for Shared, done to a module that already exists.
    ///
    /// A module only gets the chance to tick Shared on the day it is created, and by the time a
    /// second module needs its data that day is long past. This lays the same folders down, writes
    /// the same assembly and settings file, and wires the same references - so a module that gains
    /// Shared afterwards is indistinguishable from one created with it.
    ///
    /// Every step checks before it writes, so running this on a module that is already set up
    /// repairs whatever is missing and leaves the rest alone.
    /// </summary>
    internal class SharedDataInstaller
    {
        private readonly SharedAssemblyDefinition _sharedAssembly;
        private readonly AssemblyDefinitionReferences _references;

        public SharedDataInstaller() : this(new SharedAssemblyDefinition(), new AssemblyDefinitionReferences())
        {
        }

        internal SharedDataInstaller(SharedAssemblyDefinition sharedAssembly, AssemblyDefinitionReferences references)
        {
            _sharedAssembly = sharedAssembly;
            _references = references;
        }

        public ModuleInstallReport Install(ModuleRegistry registry, ModuleDescriptorEVO module, string modulePath, DirectoryStructureConfig config)
        {
            var report = new ModuleInstallReport("Add Shared");

            FolderEVO sharedFolder = FindFolderByType(config?.RootFolders, FolderEVO.FolderType.Shared);
            string sharedPath = config?.FindFullFolderPathByID(FolderEVO.FolderType.Shared, modulePath);

            if (sharedFolder == null || string.IsNullOrEmpty(sharedPath))
            {
                report.Fail("This module's folder layout has no Shared folder, so there is nothing to add.");
                return report;
            }

            string moduleAsmdefPath = FindAssemblyDefinition(modulePath);
            if (string.IsNullOrEmpty(moduleAsmdefPath))
            {
                report.Fail($"No assembly definition found in '{modulePath}', so the Shared assembly would have nothing to belong to.");
                return report;
            }

            CreateFolders(sharedPath, sharedFolder, report);

            string moduleAssemblyName = Path.GetFileNameWithoutExtension(moduleAsmdefPath);
            string sharedAssemblyName = WriteSharedAssembly(modulePath, config, moduleAssemblyName, report);

            if (string.IsNullOrEmpty(sharedAssemblyName))
            {
                report.Fail("The Shared assembly could not be written.");
                return report;
            }

            // The module has to reference its own Shared assembly: the asmdef inside Scripts/Shared
            // takes that folder out of the module's assembly, so without this the module cannot read
            // the data it just published.
            AddReference(moduleAsmdefPath, sharedAssemblyName, moduleAssemblyName, report);

            AddReferenceToChildren(registry, module, sharedAssemblyName, report);

            ModuleGenerator.AddSubAssemblyNamespaceExceptions(config, modulePath, sharedAssemblyName);
            report.WroteNamespaceSettings(sharedAssemblyName + ".csproj.DotSettings");

            AssetDatabase.Refresh();

            report.AssemblyName = sharedAssemblyName;
            return report;
        }

        /// <summary>
        /// The Shared subfolders are mandatory within Shared, so they are passed as their own
        /// selection and all of them land at once - the same thing ticking Shared in Create Module
        /// does.
        /// </summary>
        private void CreateFolders(string sharedPath, FolderEVO sharedFolder, ModuleInstallReport report)
        {
            bool existed = Directory.Exists(sharedPath);

            Directory.CreateDirectory(sharedPath);
            ModuleGenerator.CreateFoldersRecursively(sharedPath, sharedFolder.SubFolders, sharedFolder.SubFolders);

            if (!existed) report.CreatedFolders(sharedPath);
        }

        private string WriteSharedAssembly(string modulePath, DirectoryStructureConfig config, string moduleAssemblyName, ModuleInstallReport report)
        {
            string existing = _sharedAssembly.FindIn(modulePath, config);
            if (!string.IsNullOrEmpty(existing)) return existing;

            string created = _sharedAssembly.CreateFor(modulePath, config, moduleAssemblyName);
            if (!string.IsNullOrEmpty(created)) report.CreatedAssembly(created);

            return created;
        }

        /// <summary>
        /// Direct children only, which is what Create Module wires for a module created under a
        /// parent that already publishes. A grandchild reaches its own parent, not this one.
        /// </summary>
        private void AddReferenceToChildren(
            ModuleRegistry registry, ModuleDescriptorEVO module, string sharedAssemblyName, ModuleInstallReport report)
        {
            if (registry == null || module == null) return;

            var pathResolver = new ModuleAssetPathResolver();

            foreach (ModuleKind kind in new[] {ModuleKind.Sub, ModuleKind.Test, ModuleKind.Screen})
            {
                foreach (ModuleDescriptorEVO child in registry.ChildrenOf(module, kind))
                {
                    string childPath = pathResolver.ToAbsolutePath(registry.PathOf(child));
                    if (string.IsNullOrEmpty(childPath)) continue;

                    string childAsmdefPath = FindAssemblyDefinition(childPath);
                    if (string.IsNullOrEmpty(childAsmdefPath)) continue;

                    AddReference(childAsmdefPath, sharedAssemblyName, Path.GetFileNameWithoutExtension(childAsmdefPath), report);
                }
            }
        }

        private void AddReference(string asmdefPath, string sharedAssemblyName, string assemblyName, ModuleInstallReport report)
        {
            // An assembly never references itself - the Shared assembly is one of the files this
            // walk can reach when a module sits directly above it.
            if (assemblyName == sharedAssemblyName) return;

            string updated = _references.Add(File.ReadAllText(asmdefPath), sharedAssemblyName, out bool added);
            if (!added) return;

            File.WriteAllText(asmdefPath, updated);
            report.Referenced(assemblyName);
        }

        private string FindAssemblyDefinition(string modulePath)
        {
            if (string.IsNullOrEmpty(modulePath) || !Directory.Exists(modulePath)) return null;

            string[] files = Directory.GetFiles(modulePath, "*.asmdef", SearchOption.TopDirectoryOnly);

            return files.Length == 0 ? null : files[0];
        }

        private FolderEVO FindFolderByType(List<FolderEVO> folders, FolderEVO.FolderType folderType)
        {
            if (folders == null) return null;

            foreach (FolderEVO folder in folders)
            {
                if (folder.Type == folderType) return folder;

                FolderEVO found = FindFolderByType(folder.SubFolders, folderType);
                if (found != null) return found;
            }

            return null;
        }
    }
}
#endif