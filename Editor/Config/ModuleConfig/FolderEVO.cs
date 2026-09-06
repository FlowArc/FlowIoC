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

        /// <summary>
        /// Every value carries its number, because these are serialized as ints in every FolderEVO,
        /// ED_CodeGenerator and ModuleDescriptorEVO asset on disk. Written out, a value can be
        /// deleted without moving the ones below it, and a new one takes the next free number
        /// rather than whatever position it happens to be typed into.
        ///
        /// 2, 9 and 24 are gone and are not to be reused: ScreenViews, because a screen's View and
        /// Mediator are generated straight into ViewsMediators; ScreenConfigs, because a screen
        /// declares itself in its context and there is no config asset; and SharedSignals, because
        /// the public signal holder has an assembly of its own. `DirectoryStructureConfig.Heal`
        /// takes their folders out of a layout written before they went.
        ///
        /// The Shared branch mirrors a few of the Runtime folders above but has to carry types of
        /// its own. A FolderType resolves to exactly one path per module (FindFullFolderPathByID)
        /// and to exactly one GUID per module (ModuleDescriptorEVO.FolderGuids), so reusing
        /// UnityObjects or ValueObjects there would make both lookups ambiguous - and
        /// MainModuleDirectoryStructureConfigEditor already reports a locked type used twice as an
        /// error.
        /// </summary>
        public enum FolderType
        {
            Folder = 0,
            ViewsAndMediators = 1,
            RootsAndContexts = 3,
            Services = 4,
            Controllers = 5,
            Models = 6,
            UnityObjects = 7,
            ValueObjects = 8,
            SubModules = 10,
            TestModules = 11,
            ScreenModules = 12,
            Editor = 13,
            Resources = 14,
            Prefabs = 15,
            Scenes = 16,
            Systems = 17,
            Signals = 18,
            Shared = 19,
            SharedUnityObjects = 20,
            SharedValueObjects = 21,
            SharedEnums = 22,
            SharedConstants = 23,

            /// <summary>
            /// Scripts/Signals, a sibling of Scripts/Runtime and Scripts/Shared, which becomes
            /// Modules.X.Signals. The module's public signal holder lives here and nowhere else, so
            /// a module that references a neighbour's Shared assembly to read a published enum does
            /// not get that neighbour's signals in scope, and the compiler is what stops the
            /// cross-module Dispatch rather than the reader's memory. Signals above keeps its name
            /// and holds the internal holder, which never crosses an assembly boundary at all.
            /// </summary>
            PublicSignals = 25
        }

        /// <summary>
        /// The numbers of the folder types that no longer exist, for the heal that takes them out
        /// of a layout written while they did. They are numbers rather than names because the names
        /// are gone - which is the point of numbering the enum in the first place.
        /// </summary>
        internal static readonly FolderType[] RetiredFolderTypes =
        {
            (FolderType) 2, // ScreenViews
            (FolderType) 9, // ScreenConfigs
            (FolderType) 24 // SharedSignals
        };
    }
}
#endif