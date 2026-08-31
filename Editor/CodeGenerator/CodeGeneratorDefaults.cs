#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.BaseModule.ProjectPaths;
using FlowIoC.Editor.Config.ModuleConfig;

namespace FlowIoC.Editor.CodeGenerator
{
    /// <summary>
    /// What a settings asset holds before anyone edits it: the folder name each FolderType is
    /// given, and the path each module kind's DirectoryStructureConfig is written to.
    ///
    /// It is a table of its own because three callers need the same answer - the field
    /// initializers of a fresh asset, the heal that refills an asset which deserialized empty,
    /// and the Shared branch, which has to register the folder types it introduces.
    /// </summary>
    internal class CodeGeneratorDefaults
    {
        public CodeGeneratorDefaults()
        {
            FolderNames = new Dictionary<FolderConfig.FolderType, string>
            {
                {FolderConfig.FolderType.SubModules, "zSubModules"},
                {FolderConfig.FolderType.TestModules, "zTestModules"},
                {FolderConfig.FolderType.ScreenModules, "zScreenModules"},
                {FolderConfig.FolderType.ViewsAndMediators, "ViewsMediators"},
                {FolderConfig.FolderType.ScreenConfigs, "ScreenConfigs"},
                {FolderConfig.FolderType.RootsAndContexts, "RootsContexts"},
                {FolderConfig.FolderType.Services, "Services"},
                {FolderConfig.FolderType.Systems, "Systems"},
                {FolderConfig.FolderType.Signals, "Signals"},
                {FolderConfig.FolderType.Controllers, "Controllers"},
                {FolderConfig.FolderType.Models, "Models"},
                {FolderConfig.FolderType.UnityObjects, "UnityObjects"},
                {FolderConfig.FolderType.ValueObjects, "ValueObjects"},
                {FolderConfig.FolderType.Editor, "Editor"},
                {FolderConfig.FolderType.Resources, "Resources"},
                {FolderConfig.FolderType.Prefabs, "Prefabs"},
                {FolderConfig.FolderType.Scenes, "Scenes"},
                {FolderConfig.FolderType.Shared, "Shared"},
                {FolderConfig.FolderType.SharedUnityObjects, "UnityObjects"},
                {FolderConfig.FolderType.SharedValueObjects, "ValueObjects"},
                {FolderConfig.FolderType.SharedEnums, "Enums"},
                {FolderConfig.FolderType.SharedConstants, "Constants"},
                {FolderConfig.FolderType.SharedSignals, "Signals"}
            };

            SharedFolderNames = new Dictionary<FolderConfig.FolderType, string>
            {
                {FolderConfig.FolderType.Shared, FolderNames[FolderConfig.FolderType.Shared]},
                {FolderConfig.FolderType.SharedUnityObjects, FolderNames[FolderConfig.FolderType.SharedUnityObjects]},
                {FolderConfig.FolderType.SharedValueObjects, FolderNames[FolderConfig.FolderType.SharedValueObjects]},
                {FolderConfig.FolderType.SharedEnums, FolderNames[FolderConfig.FolderType.SharedEnums]},
                {FolderConfig.FolderType.SharedConstants, FolderNames[FolderConfig.FolderType.SharedConstants]},
                {FolderConfig.FolderType.SharedSignals, FolderNames[FolderConfig.FolderType.SharedSignals]}
            };

            var paths = new FlowIoCProjectPaths();

            ConfigPaths = new Dictionary<string, string>
            {
                {"Main", paths.DirectoryStructureConfig("Main")},
                {"Screen", paths.DirectoryStructureConfig("Screen")},
                {"Test", paths.DirectoryStructureConfig("Test")}
            };
        }

        /// <summary>Every folder type the generators know, and the folder each one writes.</summary>
        public IReadOnlyDictionary<FolderConfig.FolderType, string> FolderNames { get; }

        /// <summary>
        /// The subset the Shared branch introduces. A settings asset written before Shared existed
        /// has none of these, and a folder is only rename-tracked while its type is in the map.
        /// </summary>
        public IReadOnlyDictionary<FolderConfig.FolderType, string> SharedFolderNames { get; }

        /// <summary>Where each module kind's DirectoryStructureConfig asset lives.</summary>
        public IReadOnlyDictionary<string, string> ConfigPaths { get; }
    }
}

#endif
