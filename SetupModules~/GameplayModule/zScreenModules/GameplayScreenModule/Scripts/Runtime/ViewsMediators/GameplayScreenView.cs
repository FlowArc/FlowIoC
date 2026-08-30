using FlowIoC.BaseModule.Injectable.Components;
using Modules.GameplayModule.GameplayScreenModule.ViewsMediators;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using UnityEngine;

namespace Modules.GameplayModule.GameplayScreenModule.ViewsMediators
{
    [RequireComponent(typeof(ViewInjector))]
    public class GameplayScreenView : ScreenView
    {

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
