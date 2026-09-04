#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.CodeGenerator.Menus.Module;
using FlowIoC.Editor.Config.ModuleConfig;

namespace FlowIoC.Editor.ModuleScanner
{
    /// <summary>
    /// The folders a module's .csproj.DotSettings must mark as "not a namespace provider", so
    /// that a Model under Scripts/Runtime/Models lands in Modules.PlayerModule.Models rather
    /// than in Modules.PlayerModule.Scripts.Runtime.Models.
    ///
    /// It is the logic that used to sit inside NamespaceProvider as CollectNonNamespaceFolders
    /// and AddAncestorSkipFolders, with the disk and the XML taken out. Answering with a list
    /// rather than writing a file is what makes it testable, and the settings file is only ever
    /// as right as this list.
    /// </summary>
    internal class DotSettingsPlan
    {
        internal IReadOnlyList<string> SkipFoldersFor(ModuleTargetEVO module, string modulesRoot)
        {
            var skip = new List<string>(SkipFoldersInside(module?.AbsolutePath, module?.Layout));

            AddContainersAbove(module?.AbsolutePath, modulesRoot, skip);

            return skip;
        }

        /// <summary>
        /// Only the folders inside the module. The code generator asks for these when it names
        /// the namespace of a file it is about to write, and the containers a module hangs under
        /// never appear in such a namespace.
        /// </summary>
        internal IReadOnlyList<string> SkipFoldersInside(string modulePath, DirectoryStructureConfig layout)
        {
            var skip = new List<string>();

            if (layout?.RootFolders != null && !string.IsNullOrEmpty(modulePath))
                Walk(modulePath, layout.RootFolders, skip);

            return skip;
        }

        /// <summary>
        /// Only folders the layout actually lays down are considered. A folder that is neither
        /// mandatory nor optional is not part of this module type at all, and writing a setting
        /// for it would describe a folder that never exists.
        /// </summary>
        private void Walk(string basePath, List<FolderEVO> folders, List<string> skip)
        {
            foreach (FolderEVO folder in folders)
            {
                if (!folder.IsMandatory && !folder.IsOptional) continue;

                string path = Path.Combine(basePath, folder.FolderName);

                if (!folder.IsNamespaceProvider)
                    skip.Add(path);

                if (folder.SubFolders != null && folder.SubFolders.Count > 0)
                    Walk(path, folder.SubFolders, skip);
            }
        }

        /// <summary>
        /// zSubModules, zScreenModules and zTestModules hold modules; they are not part of any
        /// module's namespace. The walk stops at the modules root, because nothing above it
        /// belongs to this module - and the module folder itself is never included, since it is
        /// what the namespace is named after.
        /// </summary>
        private void AddContainersAbove(string modulePath, string modulesRoot, List<string> skip)
        {
            if (string.IsNullOrEmpty(modulePath) || string.IsNullOrEmpty(modulesRoot)) return;

            string root = modulesRoot.Replace('\\', '/');
            DirectoryInfo current = Directory.GetParent(modulePath);

            while (current != null)
            {
                string fullName = current.FullName.Replace('\\', '/');

                if (fullName.Length <= root.Length || !fullName.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                    return;

                if (Array.IndexOf(NamespaceUtility.SkipFolderNames, current.Name) >= 0)
                    skip.Add(current.FullName);

                current = current.Parent;
            }
        }
    }
}

#endif