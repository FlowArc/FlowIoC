using System;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.Extensions;
using UnityEngine;

namespace FlowIoC.ScreenModule.ViewsMediators.Screen
{
    public class ScreenBody : MonoBehaviour, IScreenBody
    {
        public Action<IScreenBody> ShowCompleted { get; set; }
        public Action<IScreenBody> HideCompleted { get; set; }
        public bool IsRegistered { get; set; }
        public ScreenVO Data { get; set; } = new();

        // A hide asked for while the show animation was still running. The service cannot tell
        // the two apart - a screen animating in is InUse as well - so the screen holds the hide
        // and plays it the moment the show reports that it is done.
        private bool _hidePending;

        public void Show()
        {
            _hidePending = false;

            if (!Data.HasShowAnimation)
            {
                ShowCompleted?.Invoke(this);
                return;
            }

            Data.AddState(ScreenState.InShowAnimation);

            ShowCompleted -= OnShowAnimationCompleted;
            ShowCompleted += OnShowAnimationCompleted;

            PlayShowAnimation();
        }

        /// <summary>
        /// Hides the screen, or holds the hide until a show animation still running has reported.
        /// The service asks for this; a hide during a show used to be dropped without a word, which
        /// left the screen on stage and - when it was being unloaded - marked as unloading for good.
        /// </summary>
        public void Hide()
        {
            if (!Data.HasState(ScreenState.InUse))
                return;

            if (Data.HasState(ScreenState.InShowAnimation))
            {
                _hidePending = true;
                return;
            }

            if (!Data.HasHideAnimation)
            {
                HideCompleted?.Invoke(this);
                return;
            }

            FlowLogger.Log(SystemLogType.Screen, "Screen HidingAnimation started! id: ", GetType().Name);
            Data.AddState(ScreenState.InHideAnimation);
            PlayHideAnimation();
        }

        private void OnShowAnimationCompleted(IScreenBody screen)
        {
            ShowCompleted -= OnShowAnimationCompleted;
            Data.RemoveState(ScreenState.InShowAnimation);

            if (!_hidePending)
                return;

            _hidePending = false;
            Hide();
        }

        /// <summary>
        /// It runs before screenBody.Show();
        /// This is the method that screen should be activated.
        /// </summary>
        public virtual void BeforeScreenActivation()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// It runs after screen RectTransform operations.
        /// </summary>
        public virtual void AfterScreenActivation() { }

        /// <summary>
        /// It runs after pool operations.
        /// This is the method that screen should be deactivated.
        /// </summary>
        public virtual void ScreenHidden()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// It runs if screenData.HasShowAnimation is true.
        /// This is the method for handling custom animations.
        /// You can run your timeline animations or you can use tween animations.
        /// </summary>
        protected virtual void PlayShowAnimation()
        {
            ShowCompleted?.Invoke(this);
        }

        /// <summary>
        /// It runs if screenData.HasHideAnimation is true.
        /// This is the method for handling custom animations.
        /// You can run your timeline animations or you can use tween animations.
        /// </summary>
        protected virtual void PlayHideAnimation()
        {
            HideCompleted?.Invoke(this);
        }
    }
}
