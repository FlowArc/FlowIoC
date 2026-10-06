#if UNITY_EDITOR
using System;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using Modules.ResourceFlyModule.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.ResourceFlyModule.ResourceFlyTestModule.ResourceFlySampleScreenModule.ViewsMediators
{
    /// <summary>
    /// Four lanes - a counter, a source, buttons - one per ready-made motion and an inventory box. It reports
    /// presses and holds references, nothing more. The screen is pooled, so the buttons are wired
    /// in OnEnable and unwired in OnDisable. No animations, so it reports shown and hidden at once.
    /// </summary>
    [RequireComponent(typeof(ViewInjector))]
    public class ResourceFlySampleScreenView : ScreenView
    {
        [SerializeField] private ResourceFlyCounterDisplay _scatterCounter;
        [SerializeField] private ResourceFlyCounterDisplay _directCounter;
        [SerializeField] private ResourceFlyCounterDisplay _curvedCounter;

        [SerializeField] private RectTransform _scatterSource;
        [SerializeField] private RectTransform _directSource;
        [SerializeField] private RectTransform _curvedSource;

        [SerializeField] private Button _scatterButton;
        [SerializeField] private Button _directButton;
        [SerializeField] private Button _curvedButton;

        [SerializeField] private ResourceFlyCounterDisplay _boxCounter;
        [SerializeField] private RectTransform _boxSource;

        [SerializeField] private Button _banknoteButton;
        [SerializeField] private Button _hatButton;
        [SerializeField] private Button _dressButton;
        [SerializeField] private Button _stonesButton;

        [SerializeField] private Sprite _hat;
        [SerializeField] private Sprite _dress;

        [SerializeField] [Min(1)] private int _amount = 25;
        [SerializeField] [Min(1)] private int _banknoteAmount = 100;
        [SerializeField] [Min(1)] private int _stoneAmount = 25;

        public Action<int> ScatterPressed;
        public Action<int> DirectPressed;
        public Action<int> CurvedPressed;
        public Action<int> BanknotePressed;
        public Action<Sprite> ItemPressed;
        public Action<int> StonesPressed;

        public ResourceFlyCounterDisplay ScatterCounter => _scatterCounter;
        public ResourceFlyCounterDisplay DirectCounter => _directCounter;
        public ResourceFlyCounterDisplay CurvedCounter => _curvedCounter;
        public ResourceFlyCounterDisplay BoxCounter => _boxCounter;

        public RectTransform ScatterSource => _scatterSource;
        public RectTransform DirectSource => _directSource;
        public RectTransform CurvedSource => _curvedSource;
        public RectTransform BoxSource => _boxSource;

        private void OnEnable()
        {
            _scatterButton.onClick.AddListener(OnScatter);
            _directButton.onClick.AddListener(OnDirect);
            _curvedButton.onClick.AddListener(OnCurved);
            _banknoteButton.onClick.AddListener(OnBanknote);
            _hatButton.onClick.AddListener(OnHat);
            _dressButton.onClick.AddListener(OnDress);
            _stonesButton.onClick.AddListener(OnStones);
        }

        private void OnDisable()
        {
            _scatterButton.onClick.RemoveListener(OnScatter);
            _directButton.onClick.RemoveListener(OnDirect);
            _curvedButton.onClick.RemoveListener(OnCurved);
            _banknoteButton.onClick.RemoveListener(OnBanknote);
            _hatButton.onClick.RemoveListener(OnHat);
            _dressButton.onClick.RemoveListener(OnDress);
            _stonesButton.onClick.RemoveListener(OnStones);
        }

        protected override void PlayShowAnimation() => ShowCompleted?.Invoke(this);

        protected override void PlayHideAnimation() => HideCompleted?.Invoke(this);

        private void OnScatter() => ScatterPressed?.Invoke(_amount);

        private void OnDirect() => DirectPressed?.Invoke(_amount);

        private void OnCurved() => CurvedPressed?.Invoke(_amount);

        private void OnBanknote() => BanknotePressed?.Invoke(_banknoteAmount);

        private void OnHat() => ItemPressed?.Invoke(_hat);

        private void OnDress() => ItemPressed?.Invoke(_dress);

        private void OnStones() => StonesPressed?.Invoke(_stoneAmount);
    }
}
#endif
