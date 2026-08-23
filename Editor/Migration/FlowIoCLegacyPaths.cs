#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.BaseModule.ProjectPaths;

namespace FlowIoC.Editor.Migration
{
    /// <summary>
    /// Where FlowIoC used to write into a project, paired with where it writes now. The legacy
    /// strings are frozen history: they describe versions already installed in the wild and must
    /// never be re-derived from <see cref="FlowIoCProjectPaths"/>.
    /// </summary>
    internal class FlowIoCLegacyPaths
    {
        private readonly FlowIoCProjectPaths _paths;

        public FlowIoCLegacyPaths(FlowIoCProjectPaths paths)
        {
            _paths = paths;
        }

        public IReadOnlyList<LegacyAssetMove> AssetMoves => new[]
        {
            new LegacyAssetMove("Assets/FlowIoC/Generated/FlowLogType.cs", _paths.FlowLogType),
            new LegacyAssetMove("Assets/FlowIoC/Generated/FlowIoC.Generated.asmref", _paths.GeneratedAsmRef),
            new LegacyAssetMove("Assets/Editor/FlowIoC/CodeGenerator/CodeGeneratorSettings.asset", _paths.CodeGeneratorSettings),
            new LegacyAssetMove("Assets/Editor/FlowIoC/CodeGenerator/MainModuleDirectoryStructureConfig.asset", _paths.DirectoryStructureConfig("Main")),
            new LegacyAssetMove("Assets/Editor/FlowIoC/CodeGenerator/ScreenModuleDirectoryStructureConfig.asset", _paths.DirectoryStructureConfig("Screen")),
            new LegacyAssetMove("Assets/Editor/FlowIoC/CodeGenerator/TestModuleDirectoryStructureConfig.asset", _paths.DirectoryStructureConfig("Test")),
            new LegacyAssetMove("Assets/Editor/FlowIoC/FolderDrawer/FlowIoCFolderDrawerConfig.asset", _paths.FolderDrawerConfig),
            new LegacyAssetMove("Assets/Resources/FlowConsoleSettings.asset", _paths.ConsoleSettings)
        };

        /// <summary>
        /// Deepest first: a folder cannot be empty until its children are gone.
        /// </summary>
        public IReadOnlyList<string> FoldersToCleanUp => new[]
        {
            "Assets/FlowIoC/Generated",
            "Assets/FlowIoC",
            "Assets/Editor/FlowIoC/CodeGenerator",
            "Assets/Editor/FlowIoC/FolderDrawer",
            "Assets/Editor/FlowIoC",
            "Assets/Editor",
            "Assets/Resources"
        };
    }
}

#endif
