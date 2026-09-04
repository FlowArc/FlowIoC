#if UNITY_EDITOR

namespace FlowIoC.Editor.ModuleScanner
{
    /// <summary>
    /// One rule about one module. Inspect answers, Fix repairs, and the two live together
    /// because the class that knows how to spot a problem is the one that knows how to undo it.
    ///
    /// Fix is only ever called for a finding this same check reported as Fixable, so it may
    /// assume the problem is there and does not need to inspect again.
    /// </summary>
    internal interface IModuleCheck
    {
        string Id { get; }
        FindingEVO Inspect(ModuleTargetEVO module);
        void Fix(ModuleTargetEVO module);
    }
}

#endif
