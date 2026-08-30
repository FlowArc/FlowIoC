using System;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using Modules.GameplayModule.Shared.Enums;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.MainModule.MainScreenModule.ViewsMediators
{
    [RequireComponent(typeof(ViewInjector))]
    public class MainScreenView : ScreenView
    {
        [SerializeField] private Button _easyButton;
        [SerializeField] private Button _mediumButton;
        [SerializeField] private Button _hardButton;

        public Action<DifficultyType> DifficultyClicked;

        private void OnEnable()
        {
            _easyButton.onClick.AddListener(() => DifficultyClicked?.Invoke(DifficultyType.Easy));
            _mediumButton.onClick.AddListener(() => DifficultyClicked?.Invoke(DifficultyType.Medium));
            _hardButton.onClick.AddListener(() => DifficultyClicked?.Invoke(DifficultyType.Hard));
        }

        private void OnDisable()
        {
            _easyButton.onClick.RemoveAllListeners();
            _mediumButton.onClick.RemoveAllListeners();
            _hardButton.onClick.RemoveAllListeners();
        }

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
