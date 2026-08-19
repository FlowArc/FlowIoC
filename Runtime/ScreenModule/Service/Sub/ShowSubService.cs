using System.Threading.Tasks;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Utils;
using FlowIoC.BaseModule.ViewsMediators.View;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.Extensions;
using FlowIoC.ScreenModule.Model.Runtime;
using FlowIoC.ScreenModule.Service.Sub.Builder;
using FlowIoC.ScreenModule.Service.Sub.Load;
using FlowIoC.ScreenModule.ViewsMediators.Screen;

namespace FlowIoC.ScreenModule.Service.Sub
{
    internal class ShowSubService
    {
        [Inject] private IScreenRuntimeModel _runtimeModel { get; set; }
        [Inject] private SetupSubService _setupService { get; set; }
        [Inject] private LoadSubService _load { get; set; }
        [Inject] private IScreenBuilderSubService _builder { get; set; }
        [Inject] private HideSubService _hide { get; set; }
        

        public async Task<T> ShowNewScreen<T>() where T : IScreenBody
        {
            ScreenVO screenData = _builder.GetAndFlushScreenData();

            T screenBody;
            FlowLogger.Log(SystemLogType.Screen, $"[ScreenService.Show.NewScreen] Showing screen new! {screenData.ScreenType.Name}");
            
            screenBody = (T) await _load.Screen(screenData);
            if (screenBody == null) return default;
            screenBody.Data = screenData;

            AfterShowScreen(screenBody);
            return screenBody;
        }

        public Task<T> ShowPooledScreen<T>() where T : IScreenBody
        {
            var screenBody = _builder.GetAndFlushScreenBody<T>();
            FlowLogger.Log(SystemLogType.Screen,
                $"[ScreenService.Show.ShowPooledScreen] Showing screen from Pool {screenBody.Data.ScreenType.Name}");

            AfterShowScreen(screenBody);
            return Task.FromResult(screenBody);
        }

        private void AfterShowScreen<T>(T screenBody) where T : IScreenBody
        {
            _runtimeModel.AddToActivePools(screenBody);
            screenBody.ShowCompleted += ShowAnimationCompleted;
            _hide.Setup(screenBody);
            _setupService.SetupScreen(screenBody);
            
            if (!screenBody.IsRegistered)
                screenBody.Register();
            
            screenBody.Show();

            //TODO: History
        }

        private void ShowAnimationCompleted(IScreenBody screenBody)
        {
            screenBody.Data.RemoveState(ScreenState.InShowAnimation);
            screenBody.ShowCompleted -= ShowAnimationCompleted;
        }

    }
}