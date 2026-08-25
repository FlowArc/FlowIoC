#if UNITY_EDITOR

namespace FlowIoC.Editor.Modules
{
    /// <summary>
    /// One registry, built the one way. Twelve call sites - menu items, the
    /// create-command/model/view windows, ModuleGenerator, NamespaceUtility - each wrote the same
    /// two constructor calls out by hand, which is eleven places to miss when what a registry
    /// needs changes.
    ///
    /// The registry is built per call rather than shared: these entry points have no common owner
    /// to hold one, and the index load behind it is an AssetDatabase lookup rather than a rescan.
    /// </summary>
    internal class ModuleRegistryFactory
    {
        public ModuleRegistry FromProject()
        {
            return new ModuleRegistry(new ModuleIndexProvider().LoadOrCreate(), new AssetDatabasePaths());
        }
    }
}

#endif
