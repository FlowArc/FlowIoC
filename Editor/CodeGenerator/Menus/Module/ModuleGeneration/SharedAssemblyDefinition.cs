#if UNITY_EDITOR
using FlowIoC.Editor.Config.ModuleConfig;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration
{
    /// <summary>
    /// The assembly a module's Shared folder becomes, and the lookup that finds it again.
    ///
    /// A module publishes its data through this assembly rather than through the one holding its
    /// Models and Commands, so a screen or sub module can read a config asset the parent authored
    /// without gaining access to the parent's logic. The asmdef sits inside Scripts/Shared, which
    /// is enough on its own to carve that folder out of the module's own assembly - Unity gives
    /// every file to the nearest asmdef above it - so the module has to reference the Shared
    /// assembly to reach its own shared data.
    ///
    /// It holds data alone. The module's public signal holder is next door in
    /// <see cref="SignalsAssemblyDefinition"/>, because a module that references this one to read
    /// a published enum must not be handed the neighbour's signals along with it.
    ///
    /// It also puts the namespace where it belongs: a value object under
    /// Scripts/Shared/Data/ValueObjects lands in Modules.PlayerModule.Shared.Data.ValueObjects
    /// and cannot collide with the Runtime type of the same name in
    /// Modules.PlayerModule.Data.ValueObjects. That needs a .csproj.DotSettings of its own -
    /// see ModuleGenerator.AddSubAssemblyNamespaceExceptions - because such a file only applies to
    /// the project it is named after.
    /// </summary>
    internal class SharedAssemblyDefinition : ModuleSubAssemblyDefinition
    {
        internal const string ASSEMBLY_SUFFIX = ".Shared";

        public SharedAssemblyDefinition() : this(new AssemblyDefinitionTemplate())
        {
        }

        internal SharedAssemblyDefinition(AssemblyDefinitionTemplate template) : base(template)
        {
        }

        protected override string AssemblySuffix => ASSEMBLY_SUFFIX;

        protected override FolderEVO.FolderType FolderType => FolderEVO.FolderType.Shared;
    }
}
#endif