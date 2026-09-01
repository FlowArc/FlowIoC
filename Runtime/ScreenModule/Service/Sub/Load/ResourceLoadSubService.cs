using System;
using System.Threading.Tasks;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Model.Config;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using UnityEngine;

namespace FlowIoC.ScreenModule.Service.Sub.Load
{
    internal class ResourceLoadSubService
    {
        [Inject] private IScreenConfigModel _configModel { get; set; }

        public async Task<IScreenBody> LoadScreen(CD_Screen config)
        {
            try
            {
                return await Task.Run(() => {
                    
                    GameObject prefab = Resources.Load<GameObject>(config.ResourcePath);
                    if (prefab == null)
                    {
                        FlowLogger.LogError(SystemLogType.Screen,$"[ResourceLoadService] Could not load prefab at path: {config.ResourcePath}");
                        Debug.LogError($"<color=magenta>[ResourceLoadService]</color> Could not load prefab at path: {config.ResourcePath}");
                        return default;
                    }

                    var screenInstance = UnityEngine.Object.Instantiate(prefab);
                    screenInstance.SetActive(false);
                    var screenBody = screenInstance.GetComponent<IScreenBody>();

                    if (screenBody == null)
                    {
                        FlowLogger.LogError(SystemLogType.Screen,$"[ResourceLoadService] IScreenBody component not found on prefab");
                        Debug.LogError($"<color=magenta>[ResourceLoadService]</color> IScreenBody component not found on prefab");
                        UnityEngine.Object.Destroy(screenInstance);
                        return default;
                    }
                    _configModel.ConfigToScreen(config, screenBody);
                    return screenBody;
                });
            }
            catch (Exception e)
            {
                FlowLogger.LogError(SystemLogType.Screen,$"[ResourceLoadService] Error loading resource screen: {e.Message}\n{e.StackTrace}");
                Debug.LogError($"<color=magenta>[ResourceLoadService]</color> Error loading resource screen: {e.Message}\n{e.StackTrace}");
                return default;
            }
        }

        public void UnloadScreen(CD_Screen config, IScreenBody screenBody)
        {
            if (screenBody == null) return;

            try
            {
                _configModel.ConfigToScreen(config, null);

                UnityEngine.Object.Destroy(screenBody.gameObject);
                Resources.UnloadUnusedAssets();
            }
            catch (Exception e)
            {
                FlowLogger.LogError(SystemLogType.Screen,$"[ScreenService] Error unloading resource screen: {e.Message}\n{e.StackTrace}");
            }
        }
    }
} 