using System.Collections.Generic;
using System.Threading.Tasks;
using FlowIoC.ConsoleModule;
using FlowIoC.PoolModule.Addressable.Components;
using FlowIoC.PoolModule.Entities;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace FlowIoC.PoolModule.Services.Sub.Load
{
    internal class AddressableLoadSubService
    {
        // Shared dictionaries across all instances to prevent redundant loading/unloading
        private static readonly Dictionary<string, AsyncOperationHandle<GameObject>> _loadedHandles = new();
        private static readonly Dictionary<string, bool> _loadingItems = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            foreach (var handle in _loadedHandles.Values)
            {
                if (handle.IsValid())
                    Addressables.Release(handle);
            }
            _loadedHandles.Clear();
            _loadingItems.Clear();
        }

        public async Task<IPoolableItem> LoadItem(AssetReferenceSpawnableObject assetReference)
        {
            if (assetReference == null || !assetReference.RuntimeKeyIsValid())
            {
                FlowLogger.LogError(SystemLogType.Pool, "[AddressableLoadService] Invalid AssetReference.");
                return null;
            }

            var key = assetReference.AssetGUID;
            
            // Prevent duplicate load attempts while one is already in progress
            if (_loadingItems.TryGetValue(key, out bool isLoading) && isLoading)
            {
                // Wait until existing load operation completes
                await _loadedHandles[key].Task;
            }

            AsyncOperationHandle<GameObject> handle;

            if (_loadedHandles.TryGetValue(key, out handle))
            {
                FlowLogger.Log(SystemLogType.Pool, $"[AddressableLoadService] Using cached handle for key: {key}");
            }
            else
            {
                _loadingItems[key] = true;
                handle = assetReference.LoadAssetAsync<GameObject>();
                _loadedHandles[key] = handle;
            }

            await handle.Task;

            // Loading finished
            if (_loadingItems.ContainsKey(key))
                _loadingItems.Remove(key);

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                var instance = Object.Instantiate(handle.Result);
                var poolableItem = instance.GetComponent<IPoolableItem>();
                if (poolableItem != null)
                {
                    return poolableItem;
                }
                FlowLogger.LogError(SystemLogType.Pool, "[AddressableLoadService] Loaded prefab does not have an IPoolableItem component.");
                Object.Destroy(instance);
            }
            else
            {
                FlowLogger.LogError(SystemLogType.Pool, $"[AddressableLoadService] Failed to load addressable asset with key: {key}");
            }
            
            return null;
        }

        public void UnloadItem(AssetReferenceSpawnableObject assetReference)
        {
            if (assetReference == null || !assetReference.RuntimeKeyIsValid()) return;
            
            var key = assetReference.AssetGUID;
            if (_loadedHandles.TryGetValue(key, out var handle))
            {
                Addressables.Release(handle);
                _loadedHandles.Remove(key);
            }

            // Ensure we also clear any loading-state residue
            if (_loadingItems.ContainsKey(key))
            {
                _loadingItems.Remove(key);
            }
        }

        public async Task PreloadItemAsync(AssetReferenceSpawnableObject assetReference)
        {
            if (assetReference == null || !assetReference.RuntimeKeyIsValid())
            {
                FlowLogger.LogError(SystemLogType.Pool, "[AddressableLoadService] Invalid AssetReference while preloading.");
                return;
            }

            var key = assetReference.AssetGUID;

            if (_loadedHandles.ContainsKey(key)) return; // already preloaded

            var handle = assetReference.LoadAssetAsync<GameObject>();
            _loadedHandles[key] = handle;
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                FlowLogger.Log(SystemLogType.Pool, $"[AddressableLoadService] Preloaded asset with key: {key}");
            }
            else
            {
                FlowLogger.LogError(SystemLogType.Pool, $"[AddressableLoadService] Failed to preload asset with key: {key}");
                _loadedHandles.Remove(key);
            }
        }
    }
} 