using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Model.Registry;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace FlowIoC.ScreenModule.Service.Sub.Load
{
    internal class AddressableLoadSubService
    {
        private readonly Dictionary<string, AsyncOperationHandle<GameObject>> _loadedScreenHandles = new();
        private readonly Dictionary<string, bool> _loadingScreens = new();

        public async Task<IScreenBody> LoadScreen(ScreenEntry entry)
        {
            string address = entry.Screen.Load.Key;

            try
            {
                FlowLogger.Log(SystemLogType.Screen, $"[AddressableLoadService] Addressable Key: {address}");

                if (_loadingScreens.TryGetValue(address, out bool isLoading) && isLoading)
                {
                    FlowLogger.LogWarning(SystemLogType.Screen, $"[AddressableLoadService] Screen {address} is already being loaded");
                    return default;
                }

                _loadingScreens[address] = true;

                if (!_loadedScreenHandles.TryGetValue(address, out AsyncOperationHandle<GameObject> handle))
                {
                    handle = Addressables.LoadAssetAsync<GameObject>(address);
                    _loadedScreenHandles[address] = handle;
                }

                await handle.Task;

                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    FlowLogger.LogError(SystemLogType.Screen, $"[AddressableLoadService] Failed to load addressable asset for {address}");
                    _loadingScreens.Remove(address);
                    return default;
                }

                GameObject screenInstance = UnityEngine.Object.Instantiate(handle.Result);
                screenInstance.SetActive(false);

                IScreenBody screenBody = screenInstance.GetComponent<IScreenBody>();
                if (screenBody == null)
                {
                    FlowLogger.LogError(SystemLogType.Screen, $"[AddressableLoadService] IScreenBody component not found on prefab for {address}");
                    UnityEngine.Object.Destroy(screenInstance);
                    _loadingScreens.Remove(address);
                    return default;
                }

                _loadingScreens.Remove(address);
                entry.Loaded = screenBody;
                return screenBody;
            }
            catch (Exception e)
            {
                FlowLogger.LogError(SystemLogType.Screen, $"[AddressableLoadService] Error loading {address}: {e.Message}\n{e.StackTrace}");
                _loadingScreens.Remove(address);
                return default;
            }
        }

        public void UnloadScreen(ScreenEntry entry, IScreenBody screenBody)
        {
            string address = entry.Screen.Load.Key;
            entry.Loaded = null;

            if (_loadedScreenHandles.TryGetValue(address, out AsyncOperationHandle<GameObject> handle))
            {
                Addressables.Release(handle);
                _loadedScreenHandles.Remove(address);
            }

            if (screenBody == null) return;

            try
            {
                UnityEngine.Object.Destroy(screenBody.gameObject);
            }
            catch (Exception e)
            {
                FlowLogger.LogError(SystemLogType.Screen, $"[ScreenService] Error unloading addressable screen: {e.Message}\n{e.StackTrace}");
            }
        }
    }
}