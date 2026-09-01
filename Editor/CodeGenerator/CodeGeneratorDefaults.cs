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
            FolderNames = new Dictionary<FolderEVO.FolderType, string>
            {
                {FolderEVO.FolderType.SubModules, "zSubModules"},
                {FolderEVO.FolderType.TestModules, "zTestModules"},
                {FolderEVO.FolderType.ScreenModules, "zScreenModules"},
                {FolderEVO.FolderType.ViewsAndMediators, "ViewsMediators"},
                {FolderEVO.FolderType.ScreenConfigs, "ScreenConfigs"},
                {FolderEVO.FolderType.RootsAndContexts, "RootsContexts"},
                {FolderEVO.FolderType.Services, "Services"},
                {FolderEVO.FolderType.Systems, "Systems"},
                {FolderEVO.FolderType.Signals, "Signals"},
                {FolderEVO.FolderType.Controllers, "Controllers"},
                {FolderEVO.FolderType.Models, "Models"},
                {FolderEVO.FolderType.UnityObjects, "UnityObjects"},
                {FolderEVO.FolderType.ValueObjects, "ValueObjects"},
                {FolderEVO.FolderType.Editor, "Editor"},
                {FolderEVO.FolderType.Resources, "Resources"},
                {FolderEVO.FolderType.Prefabs, "Prefabs"},
                {FolderEVO.FolderType.Scenes, "Scenes"},
                {FolderEVO.FolderType.Shared, "Shared"},
                {FolderEVO.FolderType.SharedUnityObjects, "UnityObjects"},
                {FolderEVO.FolderType.SharedValueObjects, "ValueObjects"},
                {FolderEVO.FolderType.SharedEnums, "Enums"},
                {FolderEVO.FolderType.SharedConstants, "Constants"},
                {FolderEVO.FolderType.SharedSignals, "Signals"}
            };

            SharedFolderNames = new Dictionary<FolderEVO.FolderType, string>
            {
                {FolderEVO.FolderType.Shared, FolderNames[FolderEVO.FolderType.Shared]},
                {FolderEVO.FolderType.SharedUnityObjects, FolderNames[FolderEVO.FolderType.SharedUnityObjects]},
                {FolderEVO.FolderType.SharedValueObjects, FolderNames[FolderEVO.FolderType.SharedValueObjects]},
                {FolderEVO.FolderType.SharedEnums, FolderNames[FolderEVO.FolderType.SharedEnums]},
                {FolderEVO.FolderType.SharedConstants, FolderNames[FolderEVO.FolderType.SharedConstants]},
                {FolderEVO.FolderType.SharedSignals, FolderNames[FolderEVO.FolderType.SharedSignals]}
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
        public IReadOnlyDictionary<FolderEVO.FolderType, string> FolderNames { get; }

        /// <summary>
        /// The subset the Shared branch introduces. A settings asset written before Shared existed
        /// has none of these, and a folder is only rename-tracked while its type is in the map.
        /// </summary>
        public IReadOnlyDictionary<FolderEVO.FolderType, string> SharedFolderNames { get; }

        /// <summary>Where each module kind's DirectoryStructureConfig asset lives.</summary>
        public IReadOnlyDictionary<string, string> ConfigPaths { get; }
    }
}

#endif
