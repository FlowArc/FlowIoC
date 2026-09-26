namespace FlowIoC.BaseModule.Root
{
    /// <summary>
    /// A sub-context whose Root entry carries settings for it. The Root hands over the entry's
    /// settings right after the context is created and before any binding phase, and the context
    /// decides what to take from them. The Root itself knows nothing about what a particular kind
    /// of sub-context is configured with, which is what keeps screen and pool concepts out of
    /// RootBase.
    ///
    /// The settings may be null - an entry written before its context took settings, or by a
    /// generator - and a context reads null as nothing configured.
    /// </summary>
    internal interface ISubContextConfigurable
    {
        void Configure(SubContextSettingsCVO settings);
    }

    /// <summary>
    /// The same, naming the settings class. It is what the inspector reads to know which settings
    /// an entry of this context carries, and what the Add Sub Context window creates for a new one.
    /// </summary>
    internal interface ISubContextConfigurable<TSettings> : ISubContextConfigurable
        where TSettings : SubContextSettingsCVO, new()
    {
    }
}
