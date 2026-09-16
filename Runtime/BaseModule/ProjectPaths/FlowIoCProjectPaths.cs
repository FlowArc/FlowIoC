#if UNITY_EDITOR

namespace FlowIoC.BaseModule.ProjectPaths
{
    /// <summary>
    /// Every file FlowIoC writes into the consuming project, derived from one root. Nothing else
    /// in the package may hardcode an <c>Assets/</c> path: changing where FlowIoC writes has to be
    /// a one-line change here.
    ///
    /// This lives in the runtime assembly rather than the editor one from the days FlowLogger
    /// loaded a settings asset by one of these paths; nothing in Runtime reads it now, and it
    /// stays here because moving it buys nothing. The whole file is editor-only because Assets
    /// paths mean nothing in a player build, and internal because Runtime/AssemblyInfo.cs already
    /// grants FlowIoC.Editor and FlowIoC.Dev.Editor.CoreTests access.
    /// </summary>
    internal class FlowIoCProjectPaths
    {
        public string Root { get; }

        public FlowIoCProjectPaths() : this("Assets/Plugins/FlowIoC")
        {
        }

        /// <summary>
        /// The same paths under another root, for a test that writes the assets FlowIoC writes
        /// without touching the project running it.
        /// </summary>
        internal FlowIoCProjectPaths(string root)
        {
            Root = root;
        }

        public string EditorRoot => Root + "/Editor";
        public string CodeGeneratorRoot => EditorRoot + "/CodeGenerator";
        public string FolderPainterRoot => EditorRoot + "/FolderPainter";

        public string CodeGeneratorSettings => CodeGeneratorRoot + "/ED_CodeGenerator.asset";
        public string ModuleIndex => CodeGeneratorRoot + "/ED_ModuleIndex.asset";
        public string FolderPainterConfig => FolderPainterRoot + "/ED_FolderPainter.asset";

        /// <summary>
        /// The per module-type directory structure config, keyed the way
        /// <c>ED_CodeGenerator.DirectoryStructureConfigPaths</c> keys them: "Main", "Screen",
        /// "Test".
        /// </summary>
        public string DirectoryStructureConfig(string configKey)
        {
            return CodeGeneratorRoot + "/ED_" + configKey + "ModuleDirectoryStructure.asset";
        }
    }
}

#endif