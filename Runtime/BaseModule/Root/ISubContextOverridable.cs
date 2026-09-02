namespace FlowIoC.BaseModule.Root
{
    /// <summary>
    /// A sub-context whose Root entry may carry configuration for it. The Root hands over the
    /// entry it was listed with, right after the context is created and before any binding phase,
    /// and the context decides what to take from it. The Root itself knows nothing about what a
    /// particular kind of sub-context can be configured with, which is what keeps screen concepts
    /// out of RootBase.
    /// </summary>
    internal interface ISubContextOverridable
    {
        void ApplyOverride(in SubContextData data);
    }
}
