#if UNITY_EDITOR

using System.Collections.Generic;

namespace FlowIoC.Editor.ModuleCards
{
    /// <summary>
    /// Everything the generated block says about one module, already reduced to strings. The
    /// collector reflects, this carries, the builder renders - so the rendering can be tested
    /// without a compiled module anywhere near it.
    ///
    /// Every list is ordered by the collector and rendered in the order it arrives. The order has
    /// to be stable or the block's hash turns over on a recompile that changed nothing.
    /// </summary>
    internal class ModuleFactsEVO
    {
        internal string Kind { get; set; }
        internal IReadOnlyList<string> Assemblies { get; set; } = new List<string>();
        internal string RootType { get; set; }
        internal string ContextType { get; set; }
        internal IReadOnlyList<string> Incoming { get; set; } = new List<string>();
        internal IReadOnlyList<string> Outgoing { get; set; } = new List<string>();
        internal IReadOnlyList<string> Publishes { get; set; } = new List<string>();
        internal IReadOnlyList<string> Services { get; set; } = new List<string>();
        internal IReadOnlyList<string> Systems { get; set; } = new List<string>();
        internal IReadOnlyList<string> SubModules { get; set; } = new List<string>();
    }
}

#endif
