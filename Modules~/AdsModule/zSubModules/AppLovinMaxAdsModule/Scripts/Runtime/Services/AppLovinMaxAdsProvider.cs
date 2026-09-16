using FlowIoC.ConsoleModule;
using Modules.AdsModule.AppLovinMaxAdsModule.Data.UnityObjects;
using Modules.AdsModule.Data.ValueObjects;
using Modules.AdsModule.Services;
using Modules.AdsModule.Shared.Data.ValueObjects;
using Modules.AdsModule.Shared.Enums;
using UnityEngine;

namespace Modules.AdsModule.AppLovinMaxAdsModule.Services
{
    /// <summary>
    /// AppLovin MAX behind the socket, a port of WayOfKings' AdServiceSub against MAX 8.6.4.
    /// The SDK key is the Integration Manager's; the ad unit ids come from CD_AppLovinMaxAds
    /// for the running store. Every MAX callback arrives on the main thread (MaxEventExecutor)
    /// and is handed to the listener one to one. In the Editor MAX shows its stub ads, so the
    /// whole path runs without a phone.
    /// </summary>
    public class AppLovinMaxAdsProvider : IAdsProvider
    {
        private const string PROVIDER_NAME = "AppLovin MAX";
        private const string CURRENCY = "USD";

        private readonly string _rewardedUnitId;
        private readonly string _interstitialUnitId;
        private IAdsProviderListener _listener;
        private bool _subscribed;
        private bool _ageRestrictedNoted;

        public string Name => PROVIDER_NAME;

        public AppLovinMaxAdsProvider(CD_AppLovinMaxAds units)
        {
            bool ios = Application.platform == RuntimePlatform.IPhonePlayer;
            _rewardedUnitId = units == null ? string.Empty : (ios ? units.Rewarded.Ios : units.Rewarded.Android) ?? string.Empty;
            _interstitialUnitId = units == null ? string.Empty : (ios ? units.Interstitial.Ios : units.Interstitial.Android) ?? string.Empty;
        }

        public void Initialize(IAdsProviderListener listener)
        {
            _listener = listener;

            if (_rewardedUnitId.Length == 0 && _interstitialUnitId.Length == 0)
            {
                FlowLogger.Log("Initialize - no ad unit id at all; CD_AppLovinMaxAds is missing from AdsServiceRoot's Shared Scriptables or empty. Nothing to initialize.");
                listener.OnInitialized(false);
                return;
            }

            if (_rewardedUnitId.Length == 0)
                FlowLogger.LogError($"Initialize - CD_AppLovinMaxAds has no Rewarded ad unit id for {Application.platform}; rewarded ads will never load.");

            if (_interstitialUnitId.Length == 0)
                FlowLogger.LogError($"Initialize - CD_AppLovinMaxAds has no Interstitial ad unit id for {Application.platform}; interstitials will never load.");

            Subscribe();
            MaxSdk.SetVerboseLogging(Debug.isDebugBuild);

            if (MaxSdk.IsInitialized())
            {
                FlowLogger.Log("Initialize - MAX was already up.");
                listener.OnInitialized(true);
                return;
            }

            MaxSdkCallbacks.OnSdkInitializedEvent += OnSdkInitialized;
            MaxSdk.InitializeSdk();
        }

        public void Load(AdFormat format)
        {
            string unitId = UnitIdOf(format);

            if (unitId.Length == 0)
            {
                FlowLogger.Log($"Load - {format} skipped: no ad unit id.");
                return;
            }

            if (format == AdFormat.Rewarded)
                MaxSdk.LoadRewardedAd(unitId);
            else
                MaxSdk.LoadInterstitial(unitId);
        }

        public bool IsReady(AdFormat format)
        {
            string unitId = UnitIdOf(format);

            if (unitId.Length == 0)
                return false;

            return format == AdFormat.Rewarded ? MaxSdk.IsRewardedAdReady(unitId) : MaxSdk.IsInterstitialReady(unitId);
        }

        public void Show(AdFormat format, string placement)
        {
            string unitId = UnitIdOf(format);

            if (format == AdFormat.Rewarded)
                MaxSdk.ShowRewardedAd(unitId, placement);
            else
                MaxSdk.ShowInterstitial(unitId, placement);
        }

        public void SetConsent(AdsConsentVO consent)
        {
            MaxSdk.SetHasUserConsent(consent.HasUserConsent);
            MaxSdk.SetDoNotSell(consent.DoNotSell);

            if (consent.IsAgeRestricted && !_ageRestrictedNoted)
            {
                _ageRestrictedNoted = true;
                FlowLogger.Log("Consent - MAX 8 takes the age-restricted flag from its dashboard, not from code; noted and not sent.");
            }
        }

        public void SetMuted(bool muted) => MaxSdk.SetMuted(muted);

        public void ShowDebugger() => MaxSdk.ShowMediationDebugger();

        private string UnitIdOf(AdFormat format) => format == AdFormat.Rewarded ? _rewardedUnitId : _interstitialUnitId;

        private void OnSdkInitialized(MaxSdkBase.SdkConfiguration configuration)
        {
            MaxSdkCallbacks.OnSdkInitializedEvent -= OnSdkInitialized;

            if (configuration.IsSuccessfullyInitialized)
                FlowLogger.Log($"Initialize - MAX is up; country {configuration.CountryCode}.");
            else
                FlowLogger.LogError("Initialize - MAX reported it did not initialize; check the SDK key in AppLovin's Integration Manager.");

            _listener.OnInitialized(configuration.IsSuccessfullyInitialized);
        }

        private void Subscribe()
        {
            if (_subscribed)
                return;

            _subscribed = true;

            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnRewardedLoaded;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnRewardedLoadFailed;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnRewardedDisplayed;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnRewardedDisplayFailed;
            MaxSdkCallbacks.Rewarded.OnAdClickedEvent += OnRewardedClicked;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnRewardedReceivedReward;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnRewardedHidden;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnRewardedRevenuePaid;

            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialLoaded;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialLoadFailed;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnInterstitialDisplayed;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnInterstitialDisplayFailed;
            MaxSdkCallbacks.Interstitial.OnAdClickedEvent += OnInterstitialClicked;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialHidden;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnInterstitialRevenuePaid;
        }

        private void OnRewardedLoaded(string unitId, MaxSdkBase.AdInfo info) => _listener.OnLoaded(AdFormat.Rewarded);
        private void OnRewardedLoadFailed(string unitId, MaxSdkBase.ErrorInfo error) => _listener.OnLoadFailed(AdFormat.Rewarded, Describe(error));
        private void OnRewardedDisplayed(string unitId, MaxSdkBase.AdInfo info) => _listener.OnDisplayed(AdFormat.Rewarded);
        private void OnRewardedDisplayFailed(string unitId, MaxSdkBase.ErrorInfo error, MaxSdkBase.AdInfo info) => _listener.OnDisplayFailed(AdFormat.Rewarded, Describe(error));
        private void OnRewardedClicked(string unitId, MaxSdkBase.AdInfo info) => _listener.OnClicked(AdFormat.Rewarded);
        private void OnRewardedReceivedReward(string unitId, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo info) => _listener.OnRewardEarned(new AdRewardVO(reward.Label, reward.Amount));
        private void OnRewardedHidden(string unitId, MaxSdkBase.AdInfo info) => _listener.OnClosed(AdFormat.Rewarded);
        private void OnRewardedRevenuePaid(string unitId, MaxSdkBase.AdInfo info) => _listener.OnRevenuePaid(Revenue(AdFormat.Rewarded, info));

        private void OnInterstitialLoaded(string unitId, MaxSdkBase.AdInfo info) => _listener.OnLoaded(AdFormat.Interstitial);
        private void OnInterstitialLoadFailed(string unitId, MaxSdkBase.ErrorInfo error) => _listener.OnLoadFailed(AdFormat.Interstitial, Describe(error));
        private void OnInterstitialDisplayed(string unitId, MaxSdkBase.AdInfo info) => _listener.OnDisplayed(AdFormat.Interstitial);
        private void OnInterstitialDisplayFailed(string unitId, MaxSdkBase.ErrorInfo error, MaxSdkBase.AdInfo info) => _listener.OnDisplayFailed(AdFormat.Interstitial, Describe(error));
        private void OnInterstitialClicked(string unitId, MaxSdkBase.AdInfo info) => _listener.OnClicked(AdFormat.Interstitial);
        private void OnInterstitialHidden(string unitId, MaxSdkBase.AdInfo info) => _listener.OnClosed(AdFormat.Interstitial);
        private void OnInterstitialRevenuePaid(string unitId, MaxSdkBase.AdInfo info) => _listener.OnRevenuePaid(Revenue(AdFormat.Interstitial, info));

        private AdRevenueVO Revenue(AdFormat format, MaxSdkBase.AdInfo info) =>
            new(PROVIDER_NAME, format, info.Placement, info.NetworkName, info.AdUnitIdentifier, info.Revenue, CURRENCY, info.RevenuePrecision);

        private string Describe(MaxSdkBase.ErrorInfo error) => error == null ? "unknown error" : $"{error.Code}: {error.Message}";
    }
}
