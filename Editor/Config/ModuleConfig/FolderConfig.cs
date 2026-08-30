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

            // Retired: a screen's View and Mediator are generated straight into ViewsMediators,
            // so no config lays this folder down any more. The value stays because every
            // FolderConfig, CodeGeneratorSettings and ModuleDescriptor asset already on disk
            // serializes these as ints - removing it would silently renumber everything below.
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
            Signals,

            // The Shared branch mirrors a few of the Runtime folders above but has to carry types
            // of its own. A FolderType resolves to exactly one path per module
            // (FindFullFolderPathByID) and to exactly one GUID per module
            // (ModuleDescriptor.FolderGuids), so reusing UnityObjects or ValueObjects here would
            // make both lookups ambiguous - and MainModuleDirectoryStructureConfigEditor already
            // reports a locked type used twice as an error. New values are appended rather than
            // inserted because every FolderConfig and CodeGeneratorSettings asset already on disk
            // serializes these as ints.
            Shared,
            SharedUnityObjects,
            SharedValueObjects,
            SharedEnums,
            SharedConstants,

            // The module's public signal holder lives here, beside the data it publishes, so a
            // Connector reaches it through Modules.X.Shared and no module assembly has to
            // reference another. Signals above keeps its name and now holds the internal holder.
            SharedSignals
        }
    }
}
#endif