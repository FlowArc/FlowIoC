#if UNITY_EDITOR
using System;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using Modules.ResourceFlyModule.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.ResourceFlyModule.ResourceFlySampleScreenModule.ViewsMediators
{
    /// <summary>
    /// Three lanes - a counter, a source, a Fly button - one per ready-made motion. It reports
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

        [SerializeField] [Min(1)] private int _amount = 25;

        public Action<int> ScatterPressed;
        public Action<int> DirectPressed;
        public Action<int> CurvedPressed;

        public ResourceFlyCounterDisplay ScatterCounter => _scatterCounter;
        public ResourceFlyCounterDisplay DirectCounter => _directCounter;
        public ResourceFlyCounterDisplay CurvedCounter => _curvedCounter;

        public RectTransform ScatterSource => _scatterSource;
        public RectTransform DirectSource => _directSource;
        public RectTransform CurvedSource => _curvedSource;

        private void OnEnable()
        {
            _scatterButton.onClick.AddListener(OnScatter);
            _directButton.onClick.AddListener(OnDirect);
            _curvedButton.onClick.AddListener(OnCurved);
        }

        private void OnDisable()
        {
            _scatterButton.onClick.RemoveListener(OnScatter);
            _directButton.onClick.RemoveListener(OnDirect);
            _curvedButton.onClick.RemoveListener(OnCurved);
        }

        protected override void PlayShowAnimation() => ShowCompleted?.Invoke(this);

        protected override void PlayHideAnimation() => HideCompleted?.Invoke(this);

        private void OnScatter() => ScatterPressed?.Invoke(_amount);

        private void OnDirect() => DirectPressed?.Invoke(_amount);

        private void OnCurved() => CurvedPressed?.Invoke(_amount);
    }
}
#endif
