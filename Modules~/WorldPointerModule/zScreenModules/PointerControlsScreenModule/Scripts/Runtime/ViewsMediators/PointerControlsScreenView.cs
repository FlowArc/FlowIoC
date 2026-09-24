#if UNITY_EDITOR
using System;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.WorldPointerModule.PointerControlsScreenModule.ViewsMediators
{
    /// <summary>
    /// The sample's buttons and the count. It reports presses and shows a number, nothing more.
    /// The screen is pooled, so the buttons are wired in OnEnable and unwired in OnDisable.
    /// </summary>
    [RequireComponent(typeof(ViewInjector))]
    public class PointerControlsScreenView : ScreenView
    {
        [SerializeField] private Button _registerButton;
        [SerializeField] private Button _changeContentButton;
        [SerializeField] private Toggle _hiddenToggle;
        [SerializeField] private Button _toggleScreenButton;
        [SerializeField] private Button _unregisterAllButton;
        [SerializeField] private Text _countLabel;

        public Action RegisterPressed;
        public Action ChangeContentPressed;
        public Action<bool> HiddenToggled;
        public Action ToggleScreenPressed;
        public Action UnregisterAllPressed;

        /// <summary>
        /// The count, and with it the two buttons that send a request to a target: with no target
        /// registered there is nothing to change or hide, so they are drawn disabled rather than
        /// left to send a request the service would refuse.
        /// </summary>
        public void ShowCount(int count)
        {
            if (_countLabel != null) _countLabel.text = $"{count} targets";

            bool hasTargets = count > 0;
            if (_changeContentButton != null) _changeContentButton.interactable = hasTargets;
            if (_hiddenToggle == null) return;

            _hiddenToggle.interactable = hasTargets;
            if (!hasTargets) _hiddenToggle.SetIsOnWithoutNotify(false);
        }

        public override void BeforeScreenActivation()
        {
            base.BeforeScreenActivation();

            ShowCount(0);
            if (_hiddenToggle != null) _hiddenToggle.SetIsOnWithoutNotify(false);
        }

        private void OnEnable()
        {
            if (_registerButton != null) _registerButton.onClick.AddListener(OnRegister);
            if (_changeContentButton != null) _changeContentButton.onClick.AddListener(OnChangeContent);
            if (_hiddenToggle != null) _hiddenToggle.onValueChanged.AddListener(OnHidden);
            if (_toggleScreenButton != null) _toggleScreenButton.onClick.AddListener(OnToggleScreen);
            if (_unregisterAllButton != null) _unregisterAllButton.onClick.AddListener(OnUnregisterAll);
        }

        private void OnDisable()
        {
            if (_registerButton != null) _registerButton.onClick.RemoveListener(OnRegister);
            if (_changeContentButton != null) _changeContentButton.onClick.RemoveListener(OnChangeContent);
            if (_hiddenToggle != null) _hiddenToggle.onValueChanged.RemoveListener(OnHidden);
            if (_toggleScreenButton != null) _toggleScreenButton.onClick.RemoveListener(OnToggleScreen);
            if (_unregisterAllButton != null) _unregisterAllButton.onClick.RemoveListener(OnUnregisterAll);
        }

        protected override void PlayShowAnimation() => ShowCompleted?.Invoke(this);

        protected override void PlayHideAnimation() => HideCompleted?.Invoke(this);

        private void OnRegister() => RegisterPressed?.Invoke();

        private void OnChangeContent() => ChangeContentPressed?.Invoke();

        private void OnHidden(bool hidden) => HiddenToggled?.Invoke(hidden);

        private void OnToggleScreen() => ToggleScreenPressed?.Invoke();

        private void OnUnregisterAll() => UnregisterAllPressed?.Invoke();
    }
}
#endif