#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlowIoC.Editor.Config.ModuleConfig
{
    [Serializable]
    public class FolderConfig
    {
        public string FolderName;

        [Tooltip("Enter the name of the folder so that the editor windows can create new classes under this folder")]
        public FolderType Type = FolderType.Folder;

        public bool IsMandatory = false;
        public bool IsOptional = false;
        public bool IsNamespaceProvider = true;

        [SerializeReference] public List<FolderConfig> SubFolders;

        public enum FolderType
        {
            Folder,
            ViewsAndMediators,
            ScreenViews,
            RootsAndContexts,
            Services,
            Controllers,
            Models,
            UnityObjects,
            ValueObjects,
            ScreenConfigs,
            SubModules,
            TestModules,
            ScreenModules,
            Editor,
            Resources,
            Prefabs,
            Scenes,

            // Append only. These values are serialized by ordinal into the directory
            // structure config assets in every consumer project, so inserting a member
            // above this line silently reassigns every folder that follows it. They are
            // also the keys of ModuleDescriptor.FolderGuids, which ModuleIndexBuilder
            // carries forward unchanged, so the same insertion would silently re-key
            // every folder GUID already on record.
            Systems,
        }
    }
}
#endif