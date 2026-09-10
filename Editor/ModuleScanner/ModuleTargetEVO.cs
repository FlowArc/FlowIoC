#if UNITY_EDITOR
using FlowIoC.Editor.Config.ModuleConfig;
using FlowIoC.Editor.Modules;

namespace FlowIoC.Editor.ModuleScanner
{
    /// <summary>
    /// Everything a module check is pointed at, assembled once by ModuleTargetFactory so that no
    /// check has to reach for Unity itself.
    ///
    /// It carries no ModuleDescriptorEVO on purpose. A descriptor comes from the stored index,
    /// and these targets come from the folder tree - which is exactly what lets the index be one
    /// of the things under test rather than a precondition of testing anything.
    /// </summary>
    internal class ModuleTargetEVO
    {
        internal string Name { get; set; }
        internal ModuleKind Kind { get; set; }
        internal string AbsolutePath { get; set; }
        internal string AssetPath { get; set; }
        internal DirectoryStructureConfig Layout { get; set; }
        internal string ParentAbsolutePath { get; set; }

        /// <summary>
        /// The name of the module this one lives in, or null for a top level module. It is what
        /// the panel hangs the row from when it draws a module under its parent.
        /// </summary>
        internal string ParentName { get; set; }

        internal string ParentSharedAssemblyName { get; set; }

        /// <summary>
        /// The Signals assembly of the module this one lives in. Only a test module is owed it -
        /// it drives the module under test through that holder - so for every other kind the
        /// field is carried and left unused rather than resolved twice.
        /// </summary>
        internal string ParentSignalsAssemblyName { get; set; }

        internal string ParentAssemblyName { get; set; }
        internal string ExpectedAssemblyName { get; set; }
        internal string ProjectRoot { get; set; }
    }
}

#endif