using System;
using Modules.AdsModule.Data.ValueObjects;
using Modules.AdsModule.Enums;
using Modules.AdsModule.Services;
using Modules.AdsModule.Shared.Enums;

namespace Modules.AdsModule.Models
{
    /// <summary>
    /// The module's state: the one plugged provider and where it is, a slot per format, the show
    /// in progress, and the switches - ads removed, consent, muted - kept as current values so a
    /// provider plugged later receives them at its own initialize and one already up receives
    /// each change as it comes. No decisions; the commands take them.
    /// </summary>
    public interface IAdsModel
    {
        AdsSettingsCVO Settings { get; }

        IAdsProvider Provider { get; }

        AdsProviderState ProviderState { get; }

        bool IsInitializeRequested { get; }

        ShowVO Current { get; }

        bool AdsRemoved { get; }

        AdsConsentVO? Consent { get; }

        bool? Muted { get; }

        /// <summary>When the last interstitial closed, in realtime seconds; negative infinity until one has.</summary>
        float LastInterstitialTime { get; }

        AdSlotVO GetSlot(AdFormat format);

        void Plug(IAdsProvider provider);

        /// <summary>Forgets the provider, both slots and the show in progress; the caller answers that show first.</summary>
        void Unplug();

        void SetProviderState(AdsProviderState state);

        void RequestInitialize();

        ShowVO BeginShow(AdFormat format, string placement, Action<AdResultVO> done);

        void EndShow();

        void SetAdsRemoved(bool removed);

        void SetConsent(AdsConsentVO consent);

        void SetMuted(bool muted);

        void MarkInterstitialShown(float time);
    }
}
