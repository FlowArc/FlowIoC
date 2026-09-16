using FlowIoC.BaseModule.Signals;
using Modules.AnalyticsModule.Data.ValueObjects;
using Modules.AnalyticsModule.Services;

namespace Modules.AnalyticsModule.Signals
{
    /// <summary>
    /// What the service says to its own commands. Dispatched by nothing outside this assembly.
    /// There is no Incoming and no Outgoing here: those two halves say what a module accepts and
    /// what it announces across a boundary, and an internal signal never crosses one.
    /// </summary>
    internal class AnalyticsInternalSignals : ISignalHolder
    {
        public Signal<IAnalyticsProvider> Plug = new();
        public Signal<IAnalyticsProvider> Unplug = new();
        public Signal Initialize = new();
        public Signal<IAnalyticsProvider, bool> ProviderReady = new();
        public Signal<AnalyticsEventVO> Log = new();
        public Signal<string, string> SetUserProperty = new();
        public Signal<string> SetUserId = new();
        public Signal<AnalyticsConsentVO> SetConsent = new();
    }
}