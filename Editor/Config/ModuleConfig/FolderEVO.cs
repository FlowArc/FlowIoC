#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace FlowIoC.Editor.Config.ModuleConfig
{
    /// <summary>
    /// This type is written through [SerializeReference], which records its full type name in the
    /// asset rather than a GUID. Renaming it to FolderEVO therefore orphaned the entries in every
    /// directory structure asset already on disk: Unity could not resolve FolderConfig, handed
    /// back a list of nulls, and Create Module threw on the first one before it could open.
    ///
    /// [MovedFrom] is what teaches Unity the old name. It has to stay for as long as an asset
    /// written before the rename might still be out there, which in a published package is
    /// indefinitely. Renaming this class again means adding another entry, not editing this one.
    /// </summary>
    [MovedFrom(true, "FlowIoC.Editor.Config.ModuleConfig", "FlowIoC.Editor", "FolderConfig")]
    [Serializable]
    public class FolderEVO
    {
        public string FolderName;

        [Tooltip("Enter the name of the folder so that the editor windows can create new classes under this folder")]
        public FolderType Type = FolderType.Folder;

        public bool IsMandatory = false;
        public bool IsOptional = false;
        public bool IsNamespaceProvider = true;

        [SerializeReference] public List<FolderEVO> SubFolders;

        public enum FolderType
        {
            Folder,
            ViewsAndMediators,

            // Retired: a screen's View and Mediator are generated straight into ViewsMediators,
            // so no config lays this folder down any more. The value stays because every
            // FolderEVO, ED_CodeGenerator and ModuleDescriptorEVO asset already on disk
            // serializes these as ints - removing it would silently renumber everything below.
            ScreenViews,

            RootsAndContexts,
            Services,
            Controllers,
            Models,
            UnityObjects,
            ValueObjects,

            // Retired: a screen declares itself in its context, so there is no config asset and no
            // folder for one. The value stays for the same reason ScreenViews' does.
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
            // (ModuleDescriptorEVO.FolderGuids), so reusing UnityObjects or ValueObjects here would
            // make both lookups ambiguous - and MainModuleDirectoryStructureConfigEditor already
            // reports a locked type used twice as an error. New values are appended rather than
            // inserted because every FolderEVO and ED_CodeGenerator asset already on disk
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