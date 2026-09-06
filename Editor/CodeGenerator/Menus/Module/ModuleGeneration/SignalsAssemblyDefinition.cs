#if UNITY_EDITOR
using FlowIoC.Editor.Config.ModuleConfig;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration
{
    /// <summary>
    /// The assembly a module's Signals folder becomes, and the lookup that finds it again.
    ///
    /// A module's public signal holder is its whole public surface, and this assembly is the only
    /// way to reach it. That is the point of it being separate from
    /// <see cref="SharedAssemblyDefinition"/>: a System or a screen legitimately references a
    /// neighbour's Shared assembly to read a published enum, and while the holder lived there the
    /// same reference put `[InjectSignal] private GameplaySignals` within reach. Now it does not
    /// compile, and signals cross between modules through a Connector because the compiler says so
    /// rather than because the reader remembered.
    ///
    /// It is not dependency free. A public signal may be generic over a published type -
    /// `Signal&lt;DifficultyType&gt;` - so the holder's assembly references whichever Shared
    /// assembly that type lives in, its own module's or another's. A Connector needs those same
    /// references for the same reason: SignalConnector.Connect&lt;T&gt; has to infer T, so without
    /// the reference the compiler reports CS0012.
    ///
    /// The folder is mandatory where Shared is optional. A module publishes data only if it has
    /// any; every module has a public surface.
    /// </summary>
    internal class SignalsAssemblyDefinition : ModuleSubAssemblyDefinition
    {
        internal const string ASSEMBLY_SUFFIX = ".Signals";

        public SignalsAssemblyDefinition() : this(new AssemblyDefinitionTemplate())
        {
        }

        internal SignalsAssemblyDefinition(AssemblyDefinitionTemplate template) : base(template)
        {
        }

        protected override string AssemblySuffix => ASSEMBLY_SUFFIX;

        protected override FolderEVO.FolderType FolderType => FolderEVO.FolderType.PublicSignals;
    }
}
#endif
