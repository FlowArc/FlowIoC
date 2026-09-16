using System;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AdsModule.Data.ValueObjects;
using Modules.AdsModule.Enums;
using Modules.AdsModule.Models;
using Modules.AdsModule.Shared.Enums;
using Modules.AdsModule.Signals;

namespace Modules.AdsModule.Services
{
    /// <summary>
    /// The surface hands every call to a command through the internal signals, so each one is a
    /// step the Flow Console shows and the decisions - is there an ad, may it show - are taken in
    /// a Command. It holds nothing; the reads come from the model.
    /// </summary>
    public class AdsService : IAdsService
    {
        [Inject] private IAdsModel _model { get; set; }
        [InjectSignal] private AdsInternalSignals _signals { get; set; }

        public AdsProviderState ProviderState => _model.ProviderState;

        public string ProviderName => _model.Provider?.Name ?? string.Empty;

        public bool IsReady(AdFormat format) =>
            _model.ProviderState == AdsProviderState.Ready && _model.GetSlot(format).State == AdLoadState.Ready;

        public bool AdsRemoved => _model.AdsRemoved;

        public void Initialize() => _signals.Initialize.Dispatch();

        public void ShowRewarded(string placement, Action<AdResultVO> done = null) =>
            _signals.Show.Dispatch(AdFormat.Rewarded, placement, done);

        public void ShowInterstitial(string placement, Action<AdResultVO> done = null) =>
            _signals.Show.Dispatch(AdFormat.Interstitial, placement, done);

        public void SetConsent(AdsConsentVO consent) => _signals.SetConsent.Dispatch(consent);

        public void SetAdsRemoved(bool removed) => _signals.SetAdsRemoved.Dispatch(removed);

        public void SetMuted(bool muted) => _signals.SetMuted.Dispatch(muted);

        public void ShowProviderDebugger() => _signals.ShowDebugger.Dispatch();

        public void Plug(IAdsProvider provider) => _signals.Plug.Dispatch(provider);

        public void Unplug(IAdsProvider provider) => _signals.Unplug.Dispatch(provider);
    }
}
