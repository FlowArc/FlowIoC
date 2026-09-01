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
            new LegacyAssetMove("Assets/Editor/FlowIoC/CodeGenerator/MainModuleDirectoryStructureConfig.asset",
                _paths.DirectoryStructureConfig("Main")),
            new LegacyAssetMove("Assets/Editor/FlowIoC/CodeGenerator/ScreenModuleDirectoryStructureConfig.asset",
                _paths.DirectoryStructureConfig("Screen")),
            new LegacyAssetMove("Assets/Editor/FlowIoC/CodeGenerator/TestModuleDirectoryStructureConfig.asset",
                _paths.DirectoryStructureConfig("Test")),
            new LegacyAssetMove("Assets/Editor/FlowIoC/FolderDrawer/FlowIoCFolderDrawerConfig.asset", _paths.FolderPainterConfig),
            new LegacyAssetMove("Assets/Resources/FlowConsoleSettings.asset", _paths.ConsoleSettings),

            // The assets FlowIoC writes took the CD_/ED_ data-type prefixes the rest of the
            // framework already used. A project installed before that keeps them under the current
            // root but under the old file names, so the rename is a move like any other.
            new LegacyAssetMove("Assets/Plugins/FlowIoC/Editor/CodeGenerator/CodeGeneratorSettings.asset", _paths.CodeGeneratorSettings),
            new LegacyAssetMove("Assets/Plugins/FlowIoC/Editor/CodeGenerator/FlowIoCModuleIndex.asset", _paths.ModuleIndex),
            new LegacyAssetMove("Assets/Plugins/FlowIoC/Editor/CodeGenerator/MainModuleDirectoryStructureConfig.asset",
                _paths.DirectoryStructureConfig("Main")),
            new LegacyAssetMove("Assets/Plugins/FlowIoC/Editor/CodeGenerator/ScreenModuleDirectoryStructureConfig.asset",
                _paths.DirectoryStructureConfig("Screen")),
            new LegacyAssetMove("Assets/Plugins/FlowIoC/Editor/CodeGenerator/TestModuleDirectoryStructureConfig.asset",
                _paths.DirectoryStructureConfig("Test")),
            new LegacyAssetMove("Assets/Plugins/FlowIoC/Editor/FolderDrawer/FlowIoCFolderDrawerConfig.asset", _paths.FolderPainterConfig),
            new LegacyAssetMove("Assets/Plugins/FlowIoC/Resources/FlowConsoleSettings.asset", _paths.ConsoleSettings),

            // The folder drawer became the Folder Painter, which moved its folder and its asset
            // in one go. A project installed between the ED_ prefixes and the rename holds the
            // config under the old name in the old folder.
            new LegacyAssetMove("Assets/Plugins/FlowIoC/Editor/FolderDrawer/ED_FolderDrawer.asset", _paths.FolderPainterConfig)
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
            "Assets/Resources",
            "Assets/Plugins/FlowIoC/Editor/FolderDrawer"
        };
    }
}

#endif