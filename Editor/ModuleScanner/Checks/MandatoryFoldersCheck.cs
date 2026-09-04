#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.Config.ModuleConfig;

namespace FlowIoC.Editor.ModuleScanner
{
    /// <summary>
    /// The folders the module's layout says must exist. Optional folders are never reported: a
    /// module without Prefabs chose that, and demanding one would put a permanent warning on half
    /// the project.
    ///
    /// An optional folder that is present is still walked into, because its own mandatory
    /// children were laid down with it - a module that publishes Shared data owes
    /// Shared/Data even though Shared itself was a choice.
    /// </summary>
    internal class MandatoryFoldersCheck : IModuleCheck
    {
        private readonly Func<string, bool> _folderExists;
        private readonly Action<string> _createFolder;

        internal MandatoryFoldersCheck() : this(Directory.Exists, path => Directory.CreateDirectory(path))
        {
        }

        internal MandatoryFoldersCheck(Func<string, bool> folderExists, Action<string> createFolder)
        {
            _folderExists = folderExists;
            _createFolder = createFolder;
        }

        public string Id => "folders";

        public FindingEVO Inspect(ModuleTargetEVO module)
        {
            List<string> missing = Missing(module);

            if (missing.Count == 0)
                return FindingEVO.Ok(Id, "Mandatory folders");

            return FindingEVO.Fixable(Id, $"Missing folders: {string.Join(", ", Names(missing))}");
        }

        public void Fix(ModuleTargetEVO module)
        {
            foreach (string path in Missing(module))
                _createFolder(path);
        }

        /// <summary>
        /// A target whose layout could not be resolved is not a module with no folders. Walking a
        /// null layout would report every folder as missing, and Fix All would then lay a tree
        /// down inside whatever path the target happens to carry.
        /// </summary>
        private List<string> Missing(ModuleTargetEVO module)
        {
            var missing = new List<string>();

            if (module?.Layout?.RootFolders == null || string.IsNullOrEmpty(module.AbsolutePath))
                return missing;

            Walk(module.AbsolutePath, module.Layout.RootFolders, missing);

            return missing;
        }

        private void Walk(string basePath, List<FolderEVO> folders, List<string> missing)
        {
            foreach (FolderEVO folder in folders)
            {
                if (!folder.IsMandatory && !folder.IsOptional) continue;

                string path = Path.Combine(basePath, folder.FolderName);
                bool exists = _folderExists(path);

                if (folder.IsMandatory && !exists)
                    missing.Add(path);

                // An optional folder that is not there takes its children with it.
                if (!folder.IsMandatory && !exists) continue;

                if (folder.SubFolders != null && folder.SubFolders.Count > 0)
                    Walk(path, folder.SubFolders, missing);
            }
        }

        /// <summary>
        /// The message names folders the way the reader sees them in the Project window, relative
        /// to the module, rather than repeating the absolute path on every entry.
        /// </summary>
        private IEnumerable<string> Names(List<string> paths)
        {
            var names = new List<string>();

            foreach (string path in paths)
                names.Add(Path.GetFileName(path));

            return names;
        }
    }
}

#endif
