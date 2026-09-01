#if UNITY_EDITOR

namespace FlowIoC.BaseModule.ProjectPaths
{
    /// <summary>
    /// Every file FlowIoC writes into the consuming project, derived from one root. Nothing else
    /// in the package may hardcode an <c>Assets/</c> path: changing where FlowIoC writes has to be
    /// a one-line change here.
    ///
    /// This lives in the runtime assembly rather than the editor one because FlowLogger is runtime
    /// code and needs the same paths, and the runtime assembly cannot reference the editor
    /// assembly. The whole file is editor-only because Assets paths mean nothing in a player
    /// build, and internal because Runtime/AssemblyInfo.cs already grants FlowIoC.Editor and
    /// FlowIoC.Tests access.
    /// </summary>
    internal class FlowIoCProjectPaths
    {
        public string Root { get; } = "Assets/Plugins/FlowIoC";

        public string EditorRoot => Root + "/Editor";
        public string CodeGeneratorRoot => EditorRoot + "/CodeGenerator";
        public string FolderDrawerRoot => EditorRoot + "/FolderDrawer";
        public string GeneratedRoot => Root + "/Generated";
        public string ResourcesRoot => Root + "/Resources";

        public string CodeGeneratorSettings => CodeGeneratorRoot + "/ED_CodeGenerator.asset";
        public string ModuleIndex => CodeGeneratorRoot + "/ED_ModuleIndex.asset";
        public string FolderDrawerConfig => FolderDrawerRoot + "/ED_FolderDrawer.asset";
        public string FlowLogType => GeneratedRoot + "/FlowLogType.cs";
        public string GeneratedAsmRef => GeneratedRoot + "/FlowIoC.Generated.asmref";
        public string ConsoleSettings => ResourcesRoot + "/CD_FlowConsole.asset";

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