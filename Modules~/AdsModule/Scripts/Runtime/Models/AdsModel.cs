using System;
using FlowIoC.BaseModule.Adapters;
using FlowIoC.BaseModule.Constructables;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AdsModule.Data.UnityObjects;
using Modules.AdsModule.Data.ValueObjects;
using Modules.AdsModule.Enums;
using Modules.AdsModule.RootsContexts;
using Modules.AdsModule.Services;
using Modules.AdsModule.Shared.Enums;
using UnityEngine;

namespace Modules.AdsModule.Models
{
    /// <summary>
    /// PostConstruct only takes CD_Ads off the Root's adapter; the adapter reports a missing
    /// filing itself, and the defaults hold so no command ever sees null settings.
    /// </summary>
    internal class AdsModel : IAdsModel, IConstructable
    {
        [Inject(nameof(AdsServiceContext))] private GameObject _root { get; set; }

        private readonly AdSlotVO[] _slots = {new(AdFormat.Rewarded), new(AdFormat.Interstitial)};
        private int _nextToken;

        public bool IsPostConstructed { get; set; }

        public bool IsDeconstructed { get; set; }

        public AdsSettingsCVO Settings { get; private set; } = new();

        public IAdsProvider Provider { get; private set; }

        public AdsProviderState ProviderState { get; private set; }

        public bool IsInitializeRequested { get; private set; }

        public ShowVO Current { get; private set; }

        public bool AdsRemoved { get; private set; }

        public AdsConsentVO? Consent { get; private set; }

        public bool? Muted { get; private set; }

        public float LastInterstitialTime { get; private set; } = float.NegativeInfinity;

        public void PostConstruct()
        {
            RootAdapter adapter = _root != null ? _root.GetComponent<RootAdapter>() : null;

            if (adapter == null)
            {
                FlowLogger.LogError("AdsServiceRoot has no RootAdapter, so CD_Ads cannot be read; the defaults hold.", _root);
                return;
            }

            CD_Ads asset = adapter.GetScriptable<CD_Ads>();
            ApplySettings(asset != null ? asset.Settings : null);
        }

        /// <summary>The settings to hold; null keeps the defaults. Internal so a test sets them without an adapter.</summary>
        internal void ApplySettings(AdsSettingsCVO settings)
        {
            if (settings != null)
                Settings = settings;
        }

        public AdSlotVO GetSlot(AdFormat format)
        {
            foreach (AdSlotVO slot in _slots)
            {
                if (slot.Format == format)
                    return slot;
            }

            throw new ArgumentOutOfRangeException(nameof(format), format, "no slot for this format");
        }

        public void Plug(IAdsProvider provider)
        {
            Provider = provider;
            ProviderState = AdsProviderState.Plugged;
        }

        public void Unplug()
        {
            Provider = null;
            ProviderState = AdsProviderState.None;
            Current = null;

            foreach (AdSlotVO slot in _slots)
            {
                slot.State = AdLoadState.Idle;
                slot.Attempt = 0;
            }
        }

        public void SetProviderState(AdsProviderState state) => ProviderState = state;

        public void RequestInitialize() => IsInitializeRequested = true;

        public ShowVO BeginShow(AdFormat format, string placement, Action<AdResultVO> done)
        {
            Current = new ShowVO(format, placement, done, ++_nextToken);
            return Current;
        }

        public void EndShow() => Current = null;

        public void SetAdsRemoved(bool removed) => AdsRemoved = removed;

        public void SetConsent(AdsConsentVO consent) => Consent = consent;

        public void SetMuted(bool muted) => Muted = muted;

        public void MarkInterstitialShown(float time) => LastInterstitialTime = time;
    }
}