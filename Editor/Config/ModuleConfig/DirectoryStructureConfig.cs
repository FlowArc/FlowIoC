#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlowIoC.Editor.Config.ModuleConfig
{
    public abstract class DirectoryStructureConfig : ScriptableObject
    {
        [field: NonSerialized] protected internal virtual List<FolderConfig> RootFolders { get; protected set; } = new List<FolderConfig>();

        protected virtual void InitializeDefaultFolderStructure()
        {
            RootFolders = new List<FolderConfig> { };
        }

        protected virtual FolderConfig CreateFolder(string folderName, FolderConfig.FolderType folderType, List<FolderConfig> subFolders = null,
            bool isMandatory = false, bool isOptional = false,
            bool isNamespaceProvider = true)
        {
            return new FolderConfig
            {
                FolderName = folderName,
                Type = folderType,
                SubFolders = subFolders ?? new List<FolderConfig>(),
                IsMandatory = isMandatory,
                IsOptional = isOptional,
                IsNamespaceProvider = isNamespaceProvider
            };
        }

        protected internal virtual string FindFullFolderPathByID(FolderConfig.FolderType folderName, string basePath)
        {
            return FindFullFolderPathByID(folderName, basePath, out _);
        }

        /// <summary>
        /// The same lookup, plus whether the <see cref="FolderConfig"/> it landed on is marked
        /// optional. The flag rides along on the walk that already finds the node rather than
        /// costing a second one, so a caller that warns about a folder missing from disk can
        /// stay quiet about the ones a module was never required to have in the first place.
        /// </summary>
        protected internal virtual string FindFullFolderPathByID(FolderConfig.FolderType folderName, string basePath, out bool isOptional)
        {
            return FindFolderPathByID(folderName, RootFolders, basePath, out isOptional);
        }

        protected virtual string FindFolderPathByID(FolderConfig.FolderType folderID, List<FolderConfig> folders, string basePath,
            out bool isOptional)
        {
            isOptional = false;
            return string.Empty;
        }
    }
}
#endif