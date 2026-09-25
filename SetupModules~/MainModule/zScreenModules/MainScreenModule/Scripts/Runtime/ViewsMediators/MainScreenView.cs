using System;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.MainModule.MainScreenModule.ViewsMediators
{
    [RequireComponent(typeof(ViewInjector))]
    public class MainScreenView : ScreenView
    {
        [SerializeField] private Button _playButton;

        public Action PlayClicked;

        private void OnEnable()
        {
            _playButton.onClick.AddListener(OnPlayClicked);
        }

        private void OnDisable()
        {
            _playButton.onClick.RemoveListener(OnPlayClicked);
        }

        private void OnPlayClicked() => PlayClicked?.Invoke();

        /// <summary>
        /// This method runs if screenData.HasShowAnimation bool is true.
        /// If you don't use custom animations delete this method.
        /// </summary>
        protected override void PlayShowAnimation()
        {
            // Do some animation
            ShowCompleted?.Invoke(this);
        }

        /// <summary>
        /// This method runs if screenData.HasHideAnimation bool is true.
        /// If you don't use custom animations delete this method.
        /// </summary>
        protected override void PlayHideAnimation()
        {
            // Do some animation
            HideCompleted?.Invoke(this);
        }
    }
}
