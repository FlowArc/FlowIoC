using System;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.TempScreens
{
    [RequireComponent(typeof(ViewInjector))]
    internal class TempScreenView : ScreenView
    {
        //@Actions

        /// <summary>
        /// A screen is pooled: hiding it deactivates the GameObject and shows the same instance
        /// again next time, so Awake and Start run once no matter how often the screen opens.
        /// Wire the buttons here and drop the listeners in OnDisable, or the second open leaves
        /// the first open's listeners attached.
        /// </summary>
        private void OnEnable()
        {
            //_sampleButton.onClick.AddListener(() => SampleClicked?.Invoke());
        }

        private void OnDisable()
        {
            //_sampleButton.onClick.RemoveAllListeners();
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