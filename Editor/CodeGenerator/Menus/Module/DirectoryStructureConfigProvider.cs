#if UNITY_EDITOR
using FlowIoC.Editor.Config.ModuleConfig;
using FlowIoC.Editor.Modules;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module
{
    /// <summary>
    /// Which directory structure a module's folders follow. Four call sites used to answer this
    /// with their own copy of the same switch, and four more with their own copy of the
    /// ModuleKind-to-ModuleType mapping that feeds it - one of them with the explaining comment
    /// copy-pasted verbatim - so a layout rule corrected in one place stayed wrong everywhere
    /// else.
    /// </summary>
    internal class DirectoryStructureConfigProvider
    {
        /// <summary>
        /// Sub-modules are laid out exactly like a Main module; there is no separate
        /// DirectoryStructureConfig for Sub, which is the whole reason ModuleKind has four
        /// members and ModuleType three.
        /// </summary>
        public ModuleType TypeOf(ModuleKind kind)
        {
            return kind switch
            {
                ModuleKind.Screen => ModuleType.Screen,
                ModuleKind.Test => ModuleType.Test,
                _ => ModuleType.Main
            };
        }

        public DirectoryStructureConfig ConfigFor(ModuleKind kind)
        {
            return ConfigFor(TypeOf(kind));
        }

        public DirectoryStructureConfig ConfigFor(ModuleType type)
        {
            return type switch
            {
                ModuleType.Main => ED_MainModuleDirectoryStructure.GetOrCreateConfig("Main"),
                ModuleType.Screen => ED_ScreenModuleDirectoryStructure.GetOrCreateConfig("Screen"),
                ModuleType.Test => ED_TestModuleDirectoryStructure.GetOrCreateConfig("Test"),
                _ => null
            };
        }

        /// <summary>
        /// The key the same three configs are recorded under in
        /// ED_CodeGenerator.DirectoryStructureConfigPaths.
        /// </summary>
        public string ConfigKeyOf(ModuleKind kind)
        {
            return TypeOf(kind).ToString();
        }
    }
}
#endif
