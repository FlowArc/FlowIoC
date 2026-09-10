#if UNITY_EDITOR

namespace FlowIoC.Editor.Modules
{
    /// <summary>
    /// What ModuleTree needs to know about a module to put it under the module it lives in.
    /// Module Scanner's rows, the targets Add Shared or Signals lists and the modules the
    /// generators offer as a parent all come from different places, and this is the part of
    /// each that the tree reads.
    /// </summary>
    internal interface IModuleTreeItem
    {
        string Name { get; }
        ModuleKind Kind { get; }

        /// <summary>The module this one lives in, by name, or null for a top level module.</summary>
        string ParentName { get; }
    }
}

#endif
