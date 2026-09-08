using System.Threading.Tasks;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Utils;
using FlowIoC.BaseModule.ViewsMediators.View;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.Extensions;
using FlowIoC.ScreenModule.Model.Runtime;
using FlowIoC.ScreenModule.Service.Sub.Load;
using FlowIoC.ScreenModule.ViewsMediators.Screen;

namespace FlowIoC.ScreenModule.Service.Sub
{
    /// <summary>
    /// Puts a screen on screen. It is handed the screen it is to show rather than asking the
    /// builder for it: the builder is one object per open, and reaching back into it for the
    /// current one is what made two opens in the same frame each other's business.
    /// </summary>
    internal class ShowSubService
    {
        [Inject] private IScreenRuntimeModel _runtimeModel { get; set; }
        [Inject] private SetupSubService _setupService { get; set; }
        [Inject] private LoadSubService _load { get; set; }
        [Inject] private HideSubService _hide { get; set; }

        public async Task<T> ShowNewScreen<T>(ScreenVO screenData) where T : IScreenBody
        {
            FlowLogger.Log(SystemLogType.Screen, $"[ShowSubService.ShowNewScreen][screen({screenData.ScreenType.Name})]");

            T screenBody = (T) await _load.Screen(screenData);
            if (screenBody == null) return default;

            screenBody.Data = screenData;

            AfterShowScreen(screenBody);
            return screenBody;
        }

        /// <summary>
        /// Nothing here is awaited - the instance is already built and parked - so this answers
        /// straight away rather than handing back a task that is already finished.
        /// </summary>
        public T ShowPooledScreen<T>(IScreenBody screenBody) where T : IScreenBody
        {
            if (screenBody == null)
            {
                FlowLogger.LogError(SystemLogType.Screen, "[ScreenService.Show.ShowPooledScreen] Screen body is null");
                return default;
            }

            FlowLogger.Log(SystemLogType.Screen,
                $"[ShowSubService.ShowPooledScreen][screen({screenBody.Data.ScreenType.Name})]");

            AfterShowScreen(screenBody);
            return (T) screenBody;
        }

        private void AfterShowScreen<T>(T screenBody) where T : IScreenBody
        {
            _runtimeModel.AddToActivePools(screenBody);

            // Dropped first for the same reason the hide side does it: a screen shown again before
            // its last show finished would otherwise carry two subscriptions.
            screenBody.ShowCompleted -= ShowAnimationCompleted;
            screenBody.ShowCompleted += ShowAnimationCompleted;

            _hide.Setup(screenBody);
            _setupService.SetupScreen(screenBody);

            if (!screenBody.IsRegistered)
                screenBody.Register();

            screenBody.Show();
        }

        private void ShowAnimationCompleted(IScreenBody screenBody)
        {
            screenBody.Data.RemoveState(ScreenState.InShowAnimation);
            screenBody.ShowCompleted -= ShowAnimationCompleted;
        }
    }
}