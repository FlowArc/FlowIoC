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
            Systems,
            Signals
        }
    }
}
#endif