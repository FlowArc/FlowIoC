using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Model.Config;
using FlowIoC.ScreenModule.Service.Sub.Load;
using FlowIoC.ScreenModule.ViewsMediators.Screen;

namespace FlowIoC.ScreenModule.Service.Sub
{
    internal class DisposeSubService
    {
        [Inject] private IScreenConfigModel _configModel { get; set; }
        [Inject] private AddressableLoadSubService _addressableLoadService{ get; set; }
        [Inject] private ResourceLoadSubService _resourceLoadService{ get; set; }
        [Inject] private DirectPrefabLoadSubService _directPrefabLoadService{ get; set; }

        public void Screen(IScreenBody screenBody)
        {
            var config = _configModel.GetScreenConfig(screenBody.Data.ManagerId, screenBody.Data.ScreenType);
            UnloadByLoadType(config, screenBody);   
        }

        private void UnloadByLoadType( CD_Screen config, IScreenBody screenBody)
        {
            switch (config.LoadType)
            {
                case ScreenLoadType.Addressable:
                    _addressableLoadService.UnloadScreen(config, screenBody);
                    break;

                case ScreenLoadType.Resource:
                    _resourceLoadService.UnloadScreen(config, screenBody);
                    break;

                case ScreenLoadType.DirectPrefab:
                    _directPrefabLoadService.UnloadScreen(config, screenBody);
                    break;

                default:
                    FlowLogger.LogError(SystemLogType.Screen, $"[ScreenService] Unknown load type: {config.LoadType} for screen {screenBody.GetType().Name}");
                    break;
            }
        }
    }
}