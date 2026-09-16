using System.Collections.Generic;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AnalyticsModule.Data.ValueObjects;
using Modules.AnalyticsModule.Models;
using Modules.AnalyticsModule.Signals;

namespace Modules.AnalyticsModule.Services
{
    /// <summary>
    /// The surface hands every call to a command through the internal signals, so each one is a
    /// step the Flow Console shows and the decisions - which slot is ready, what to queue - are
    /// taken in a Command. It holds nothing; the one read comes from the model.
    /// </summary>
    public class AnalyticsService : IAnalyticsService
    {
        [Inject] private IAnalyticsModel _model { get; set; }
        [InjectSignal] private AnalyticsInternalSignals _signals { get; set; }

        public IReadOnlyList<ProviderSlotVO> Slots => _model.Slots;

        public void Log(AnalyticsEventVO analyticsEvent) => _signals.Log.Dispatch(analyticsEvent);

        public void SetUserProperty(string name, string value) => _signals.SetUserProperty.Dispatch(name, value);

        public void SetUserId(string userId) => _signals.SetUserId.Dispatch(userId);

        public void SetConsent(AnalyticsConsentVO consent) => _signals.SetConsent.Dispatch(consent);

        public void Plug(IAnalyticsProvider provider) => _signals.Plug.Dispatch(provider);

        public void Unplug(IAnalyticsProvider provider) => _signals.Unplug.Dispatch(provider);
    }
}
