using System;
using System.Threading.Tasks;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Model.Config;
using FlowIoC.ScreenModule.ViewsMediators.Screen;

namespace FlowIoC.ScreenModule.Service.Sub.Load
{
    internal class DirectPrefabLoadSubService
    {
        [Inject] private IScreenConfigModel _configModel { get; set; }

        public async Task<IScreenBody> LoadScreen(ScreenConfig config)
        {
            try
            {
                await Task.Yield();
                
                var screenInstance = UnityEngine.Object.Instantiate(config.DirectPrefab);
                screenInstance.SetActive(false);
                var screenBody = screenInstance.GetComponent<IScreenBody>();
                if (screenBody == null)
                {
                    FlowLogger.LogError(SystemLogType.Screen, "[ScreenService.Load][DirectPrefabLoad] Error! Missing IScreenBody on loaded screen");
                    UnityEngine.Object.Destroy(screenInstance);
                    return default;
                }

                FlowLogger.Log(SystemLogType.Screen, $"[ScreenService.Load][DirectPrefabLoad] {screenBody.gameObject.name} is loaded in test mode");

                _configModel.ConfigToScreen(config, screenBody);
                return screenBody;
            }
            catch (Exception e)
            {
                FlowLogger.LogError(SystemLogType.Screen, $"[ScreenService.Load][DirectPrefabLoad] Error loading screen: {e.Message}\n{e.StackTrace}");
                return default;
            }
        }

        public void UnloadScreen(ScreenConfig config, IScreenBody screenBody)
        {
            if (screenBody == null) return;

            try
            {
                _configModel.ConfigToScreen(config, null);
                UnityEngine.Object.Destroy(screenBody.gameObject);
            }
            catch (Exception e)
            {
                FlowLogger.LogError(SystemLogType.Screen,
                    $"[ScreenService.UnLoad][DirectPrefabLoad] Error!: {e.Message}\n{e.StackTrace}");
            }
        }
    }
}