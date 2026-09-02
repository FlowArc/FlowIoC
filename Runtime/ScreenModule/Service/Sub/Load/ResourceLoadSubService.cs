using System;
using System.Threading.Tasks;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Model.Registry;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using UnityEngine;

namespace FlowIoC.ScreenModule.Service.Sub.Load
{
    internal class ResourceLoadSubService
    {
        public async Task<IScreenBody> LoadScreen(ScreenEntry entry)
        {
            string path = entry.Screen.Load.Key;

            try
            {
                await Task.Yield();

                GameObject prefab = Resources.Load<GameObject>(path);
                if (prefab == null)
                {
                    FlowLogger.LogError(SystemLogType.Screen, $"[ResourceLoadService] Could not load prefab at path: {path}");
                    return default;
                }

                GameObject screenInstance = UnityEngine.Object.Instantiate(prefab);
                screenInstance.SetActive(false);

                IScreenBody screenBody = screenInstance.GetComponent<IScreenBody>();
                if (screenBody == null)
                {
                    FlowLogger.LogError(SystemLogType.Screen, $"[ResourceLoadService] IScreenBody component not found on prefab at {path}");
                    UnityEngine.Object.Destroy(screenInstance);
                    return default;
                }

                entry.Loaded = screenBody;
                return screenBody;
            }
            catch (Exception e)
            {
                FlowLogger.LogError(SystemLogType.Screen, $"[ResourceLoadService] Error loading resource screen: {e.Message}\n{e.StackTrace}");
                return default;
            }
        }

        public void UnloadScreen(ScreenEntry entry, IScreenBody screenBody)
        {
            entry.Loaded = null;

            if (screenBody == null) return;

            try
            {
                UnityEngine.Object.Destroy(screenBody.gameObject);
                Resources.UnloadUnusedAssets();
            }
            catch (Exception e)
            {
                FlowLogger.LogError(SystemLogType.Screen, $"[ScreenService] Error unloading resource screen: {e.Message}\n{e.StackTrace}");
            }
        }
    }
}