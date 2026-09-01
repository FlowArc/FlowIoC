using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Model.Config;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace FlowIoC.ScreenModule.Service.Sub.Load
{
    internal class AddressableLoadSubService
    {
        [Inject] private IScreenConfigModel _configModel { get; set; }

        private Dictionary<string, AsyncOperationHandle<GameObject>> _loadedScreenHandles = new();
        private Dictionary<string, bool> _loadingScreens = new();

        public async Task<IScreenBody> LoadScreen(CD_Screen config)
        {
            try
            {
                FlowLogger.Log(SystemLogType.Screen, $"[AddressableLoadService] Addressable Key: {config.AddressableKey}");

                if (_loadingScreens.TryGetValue(config.AddressableKey, out bool isLoading) && isLoading)
                {
                    FlowLogger.LogWarning(SystemLogType.Screen, $"[AddressableLoadService] Screen {config.AddressableKey} is already being loaded");
                    return default;
                }

                _loadingScreens[config.AddressableKey] = true;

                AsyncOperationHandle<GameObject> handle;
                if (_loadedScreenHandles.TryGetValue(config.AddressableKey, out var existingHandle))
                {
                    FlowLogger.Log(SystemLogType.Screen, $"[AddressableLoadService] Using existing handle for {config.AddressableKey}");
                    handle = existingHandle;
                }
                else
                {
                    if (string.IsNullOrEmpty(config.AddressableKey))
                    {
                        FlowLogger.LogError(SystemLogType.Screen, $"[AddressableLoadService] Addressable key is empty for {config.AddressableKey}");
                        _loadingScreens.Remove(config.AddressableKey);
                        return default;
                    }

                    handle = Addressables.LoadAssetAsync<GameObject>(config.AddressableKey);
                    _loadedScreenHandles[config.AddressableKey] = handle;
                }

                try
                {
                    await handle.Task;

                    if (handle.Status != AsyncOperationStatus.Succeeded)
                    {
                        FlowLogger.LogError(SystemLogType.Screen, $"[AddressableLoadService] Failed to load addressable asset for {config.AddressableKey}");
                        _loadingScreens.Remove(config.AddressableKey);
                        return default;
                    }

                    var screenInstance = UnityEngine.Object.Instantiate(handle.Result);
                    screenInstance.SetActive(false);
                    var screenBody = screenInstance.GetComponent<IScreenBody>();
                    if (screenBody == null)
                    {
                        FlowLogger.LogError(SystemLogType.Screen, $"[AddressableLoadService] IScreenBody component not found on prefab for {config.AddressableKey}");
                        UnityEngine.Object.Destroy(screenInstance);
                        _loadingScreens.Remove(config.AddressableKey);
                        return default;
                    }
                    _loadingScreens.Remove(config.AddressableKey);
                    _configModel.ConfigToScreen(config, screenBody);

                    return screenBody;
                }
                catch (Exception e)
                {
                    FlowLogger.LogError(SystemLogType.Screen, $"[AddressableLoadService] Error during addressable load: {e.Message}\n{e.StackTrace}");
                    _loadingScreens.Remove(config.AddressableKey);
                    return default;
                }
            }
            catch (Exception e)
            {
                FlowLogger.LogError(SystemLogType.Screen, $"[AddressableLoadService] Critical error loading screen: {e.Message}\n{e.StackTrace}");
                if (_loadingScreens.ContainsKey(config.AddressableKey))
                {
                    _loadingScreens.Remove(config.AddressableKey);
                }

                return default;
            }
        }

        public void UnloadScreen(CD_Screen config, IScreenBody screenBody)
        {
            if (_loadedScreenHandles.TryGetValue(config.AddressableKey, out var handle))
            {
                _configModel.ConfigToScreen(config, null);

                Addressables.Release(handle);
                _loadedScreenHandles.Remove(config.AddressableKey);
            }
            if (screenBody == null) return;
            try
            {
                _configModel.ConfigToScreen(config, null);

                UnityEngine.Object.Destroy(screenBody.gameObject);
            }
            catch (Exception e)
            {
                FlowLogger.LogError(SystemLogType.Screen,$"[ScreenService] Error unloading resource screen: {e.Message}\n{e.StackTrace}");
            }
        }
    }
}