namespace Modules.AnalyticsModule.Enums
{
    /// <summary>
    /// Where a plugged provider is. Plugged: listed, not yet asked to initialize (the service has
    /// not launched). Initializing: asked, no answer yet - events queue. Ready: everything goes
    /// straight through. Failed: the SDK said no - events are skipped.
    /// </summary>
    public enum AnalyticsProviderState
    {
        Plugged = 0,
        Initializing = 1,
        Ready = 2,
        Failed = 3
    }
}
