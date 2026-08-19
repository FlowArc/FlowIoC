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
        /// This method runs if screenData.HasShowAnimation bool is true.
        /// If you dont use custom animations delete this method.
        /// </summary>
        protected override void PlayShowAnimation()
        {
            // Do some animation
            ShowCompleted?.Invoke(this);
        }

        /// <summary>
        /// This method runs if screenData.HasHideAnimation bool is true.
        /// If you dont use custom animations delete this method.
        /// </summary>
        protected override void PlayHideAnimation()
        {
            // Do some animation
            HideCompleted?.Invoke(this);
        }
    }
}