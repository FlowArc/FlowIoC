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

        protected virtual FolderConfig CreateFolder(string folderName, FolderConfig.FolderType folderType, List<FolderConfig> subFolders = null, bool isMandatory = false, bool isOptional = false,
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
            return FindFolderPathByID(folderName, RootFolders, basePath);
        }

        protected virtual string FindFolderPathByID(FolderConfig.FolderType folderID, List<FolderConfig> folders, string basePath)
        {
            return string.Empty;
        }
    }
}
#endif