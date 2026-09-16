#if UNITY_EDITOR

using System;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.BaseModule.ViewsMediators.View;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.AdsModule.AdsTestModule.ViewsMediators
{
    /// <summary>Scene references and raw input: ten buttons, three toggles and a label.</summary>
    [RequireComponent(typeof(ViewInjector))]
    public class AdsTestView : MonoBehaviour, IView
    {
        public bool IsRegistered { get; set; }

        [SerializeField] private Button _providerReadyButton;
        [SerializeField] private Button _providerFailedButton;
        [SerializeField] private Button _initializeButton;
        [SerializeField] private Button _showRewardedButton;
        [SerializeField] private Button _showInterstitialButton;
        [SerializeField] private Button _rewardAndCloseButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _failToDisplayButton;
        [SerializeField] private Button _payRevenueButton;
        [SerializeField] private Button _consentAllButton;
        [SerializeField] private Toggle _loadsFailToggle;
        [SerializeField] private Toggle _silentShowToggle;
        [SerializeField] private Toggle _adsRemovedToggle;
        [SerializeField] private Text _statusLabel;

        public Action<bool> OnAnswerProvider;
        public Action OnInitialize;
        public Action OnShowRewarded;
        public Action OnShowInterstitial;
        public Action OnRewardAndClose;
        public Action OnClose;
        public Action OnFailToDisplay;
        public Action OnPayRevenue;
        public Action OnConsentAll;
        public Action<bool> OnLoadsFail;
        public Action<bool> OnSilentShow;
        public Action<bool> OnAdsRemoved;

        public void SetStatus(string text)
        {
            if (_statusLabel != null)
                _statusLabel.text = text;
        }

        private void OnEnable()
        {
            _providerReadyButton?.onClick.AddListener(ProviderReady);
            _providerFailedButton?.onClick.AddListener(ProviderFailed);
            _initializeButton?.onClick.AddListener(Initialize);
            _showRewardedButton?.onClick.AddListener(ShowRewarded);
            _showInterstitialButton?.onClick.AddListener(ShowInterstitial);
            _rewardAndCloseButton?.onClick.AddListener(RewardAndClose);
            _closeButton?.onClick.AddListener(Close);
            _failToDisplayButton?.onClick.AddListener(FailToDisplay);
            _payRevenueButton?.onClick.AddListener(PayRevenue);
            _consentAllButton?.onClick.AddListener(ConsentAll);
            _loadsFailToggle?.onValueChanged.AddListener(LoadsFail);
            _silentShowToggle?.onValueChanged.AddListener(SilentShow);
            _adsRemovedToggle?.onValueChanged.AddListener(AdsRemoved);
        }

        private void OnDisable()
        {
            _providerReadyButton?.onClick.RemoveListener(ProviderReady);
            _providerFailedButton?.onClick.RemoveListener(ProviderFailed);
            _initializeButton?.onClick.RemoveListener(Initialize);
            _showRewardedButton?.onClick.RemoveListener(ShowRewarded);
            _showInterstitialButton?.onClick.RemoveListener(ShowInterstitial);
            _rewardAndCloseButton?.onClick.RemoveListener(RewardAndClose);
            _closeButton?.onClick.RemoveListener(Close);
            _failToDisplayButton?.onClick.RemoveListener(FailToDisplay);
            _payRevenueButton?.onClick.RemoveListener(PayRevenue);
            _consentAllButton?.onClick.RemoveListener(ConsentAll);
            _loadsFailToggle?.onValueChanged.RemoveListener(LoadsFail);
            _silentShowToggle?.onValueChanged.RemoveListener(SilentShow);
            _adsRemovedToggle?.onValueChanged.RemoveListener(AdsRemoved);
        }

        private void ProviderReady() => OnAnswerProvider?.Invoke(true);
        private void ProviderFailed() => OnAnswerProvider?.Invoke(false);
        private void Initialize() => OnInitialize?.Invoke();
        private void ShowRewarded() => OnShowRewarded?.Invoke();
        private void ShowInterstitial() => OnShowInterstitial?.Invoke();
        private void RewardAndClose() => OnRewardAndClose?.Invoke();
        private void Close() => OnClose?.Invoke();
        private void FailToDisplay() => OnFailToDisplay?.Invoke();
        private void PayRevenue() => OnPayRevenue?.Invoke();
        private void ConsentAll() => OnConsentAll?.Invoke();
        private void LoadsFail(bool on) => OnLoadsFail?.Invoke(on);
        private void SilentShow(bool on) => OnSilentShow?.Invoke(on);
        private void AdsRemoved(bool on) => OnAdsRemoved?.Invoke(on);
    }
}

#endif
