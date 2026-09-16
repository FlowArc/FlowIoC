#if UNITY_EDITOR

using System;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.BaseModule.ViewsMediators.View;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.AnalyticsModule.AnalyticsTestModule.ViewsMediators
{
    /// <summary>Scene references and raw input: eight buttons and a label.</summary>
    [RequireComponent(typeof(ViewInjector))]
    public class AnalyticsTestView : MonoBehaviour, IView
    {
        public bool IsRegistered { get; set; }

        [SerializeField] private Button _logTestEventButton;
        [SerializeField] private Button _logStepButton;
        [SerializeField] private Button _setPropertyButton;
        [SerializeField] private Button _setIdButton;
        [SerializeField] private Button _consentGrantedButton;
        [SerializeField] private Button _consentDeniedButton;
        [SerializeField] private Button _providerReadyButton;
        [SerializeField] private Button _providerFailedButton;
        [SerializeField] private Text _statusLabel;

        public Action OnLogTestEvent;
        public Action OnLogStep;
        public Action OnSetProperty;
        public Action OnSetId;
        public Action OnConsentGranted;
        public Action OnConsentDenied;
        public Action<bool> OnAnswerProvider;

        public void SetStatus(string text)
        {
            if (_statusLabel != null)
                _statusLabel.text = text;
        }

        private void OnEnable()
        {
            _logTestEventButton?.onClick.AddListener(LogTestEvent);
            _logStepButton?.onClick.AddListener(LogStep);
            _setPropertyButton?.onClick.AddListener(SetProperty);
            _setIdButton?.onClick.AddListener(SetId);
            _consentGrantedButton?.onClick.AddListener(ConsentGranted);
            _consentDeniedButton?.onClick.AddListener(ConsentDenied);
            _providerReadyButton?.onClick.AddListener(ProviderReady);
            _providerFailedButton?.onClick.AddListener(ProviderFailed);
        }

        private void OnDisable()
        {
            _logTestEventButton?.onClick.RemoveListener(LogTestEvent);
            _logStepButton?.onClick.RemoveListener(LogStep);
            _setPropertyButton?.onClick.RemoveListener(SetProperty);
            _setIdButton?.onClick.RemoveListener(SetId);
            _consentGrantedButton?.onClick.RemoveListener(ConsentGranted);
            _consentDeniedButton?.onClick.RemoveListener(ConsentDenied);
            _providerReadyButton?.onClick.RemoveListener(ProviderReady);
            _providerFailedButton?.onClick.RemoveListener(ProviderFailed);
        }

        private void LogTestEvent() => OnLogTestEvent?.Invoke();
        private void LogStep() => OnLogStep?.Invoke();
        private void SetProperty() => OnSetProperty?.Invoke();
        private void SetId() => OnSetId?.Invoke();
        private void ConsentGranted() => OnConsentGranted?.Invoke();
        private void ConsentDenied() => OnConsentDenied?.Invoke();
        private void ProviderReady() => OnAnswerProvider?.Invoke(true);
        private void ProviderFailed() => OnAnswerProvider?.Invoke(false);
    }
}

#endif
