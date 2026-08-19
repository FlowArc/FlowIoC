using System;
using FlowIoC.BaseModule.ViewsMediators.View;
using FlowIoC.ScreenModule.Data;

namespace FlowIoC.ScreenModule.ViewsMediators.Screen
{
    public interface IScreenBody : IView
    {
        ScreenVO Data { get; set; }
        Action<IScreenBody> ShowCompleted { get; set; }
        Action<IScreenBody> HideCompleted { get; set; }
        void Show();
        void Hide();
        void ScreenHidden();
        void BeforeScreenActivation();
        void AfterScreenActivation();
    }
}