using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlowIoC.BaseModule.ViewsMediators.Utils;
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
                FlowLogger.Log(SystemLogType.Screen, $"{address} requested from Addressables");

                // A second load of an address already in flight used to be refused with a warning and
                // a null screen. Nobody asked for that: the caller meant the load, and the same
                // address is legitimately loaded twice when one screen is registered at two
                // managers. It waits for the handle already loading instead, the way the pool's
                // loader does.
                if (_loadingScreens.TryGetValue(address, out bool isLoading) && isLoading
                                                                             && _loadedScreenHandles.TryGetValue(address,
                                                                                 out AsyncOperationHandle<GameObject> inFlight))
                {
                    FlowLogger.Log(SystemLogType.Screen,
                        $"[AddressableLoadService] {address} is already loading - waiting for that load to finish");

                    await inFlight.Task;

                    // Two loads of one entry are one screen, and a second instance of it is a leak
                    // nothing holds. Two entries sharing an address each still get their own.
                    if (entry.Loaded != null)
                        return entry.Loaded;
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

            // Not == null: an IScreenBody is an interface, so that compares the managed reference
            // and says a screen Unity destroyed on play mode exit is still there.
            if (!screenBody.IsAlive()) return;

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