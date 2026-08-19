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
        
        public void Show()
        {
            if (!Data.HasShowAnimation)
            {
                ShowCompleted?.Invoke(this);
                return;
            }

            Data.AddState(ScreenState.InShowAnimation);
            PlayShowAnimation();
        }

        // It runs by ScreenModel
        /// <summary>
        /// Check Closing Animation And SendTo Pool
        /// Hide will be trigger via ScreenService
        /// </summary>
        public void Hide()
        {
            if (Data.State != ScreenState.InUse)
                return;
                
            if (!Data.HasHideAnimation)
            {
                HideCompleted?.Invoke(this);
                return;
            }

            FlowLogger.Log(SystemLogType.Screen, "Screen HidingAnimation started! id: " + this.GetType().Name);
            Data.AddState(ScreenState.InHideAnimation);
            PlayHideAnimation();
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
        /// It runs if screenData.HasHideAnimation is true.
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