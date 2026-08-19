using System.Threading.Tasks;
using FlowIoC.AssetModule.Data;
using FlowIoC.AssetModule.Extensions;
using FlowIoC.AssetModule.Model;
using FlowIoC.AssetModule.Signals;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace FlowIoC.AssetModule.Service.Sub
{
    internal sealed class AssetLoadSubService
    {
        [Inject] private IAssetRegistryModel _registry { get; set; }
        [Inject] private AssetReleaseSubService _release { get; set; }
        [InjectSignal] private AssetSignals _signals { get; set; }

        public async Task<T> LoadAssetAsync<T>(object key, string groupId = null)
        {
            if (!AssetKeyExtensions.TryNormalize(key, out var regKey, out var runtimeKey))
            {
                FlowLogger.LogError(SystemLogType.Asset, "[AssetService] LoadAssetAsync: invalid key.");
                return default;
            }

            var entry = _registry.GetOrCreateEntry(regKey);
            AddOwnership(entry, regKey, groupId);

            if (entry.Result is T cached)
                return cached;

            if (!_registry.InFlight.TryGetValue(regKey, out var task))
            {
                task = LoadInternalAsync<T>(regKey, runtimeKey, entry);
                _registry.InFlight[regKey] = task;
            }

            var result = await task;
            return result is T typed ? typed : default;
        }

        public T LoadAsset<T>(object key, string groupId = null)
        {
            if (!AssetKeyExtensions.TryNormalize(key, out var regKey, out var runtimeKey))
            {
                FlowLogger.LogError(SystemLogType.Asset, "[AssetService] LoadAsset: invalid key.");
                return default;
            }

            var entry = _registry.GetOrCreateEntry(regKey);
            AddOwnership(entry, regKey, groupId);

            if (entry.Result is T cached)
                return cached;

            AsyncOperationHandle<T> handle;
            if (entry.Handle.IsValid())
            {
                handle = entry.Handle.Convert<T>();
            }
            else
            {
                handle = Addressables.LoadAssetAsync<T>(runtimeKey);
                entry.Handle = handle;
                entry.AssetType = typeof(T);
            }

            var result = handle.WaitForCompletion();

            if (handle.Status != AsyncOperationStatus.Succeeded || result == null)
            {
                FlowLogger.LogError(SystemLogType.Asset, $"[AssetService] LoadAsset failed: {regKey}");
                _signals.OutGoing.AssetLoadFailed.Dispatch(regKey);
                _release.HardRelease(regKey);
                return default;
            }

            entry.Result = result;
            return result;
        }

        public bool TryGetAsset<T>(object key, out T asset)
        {
            asset = default;
            if (!AssetKeyExtensions.TryNormalize(key, out var regKey, out _)) return false;

            if (_registry.Entries.TryGetValue(regKey, out var entry) && entry.Result is T typed)
            {
                asset = typed;
                return true;
            }

            return false;
        }

        public void AddOwnership(AssetEntryVO entry, string regKey, string groupId)
        {
            if (groupId == null)
            {
                entry.UnscopedClaims++;
                return;
            }

            if (entry.Owners.Add(groupId))
                _registry.GetOrCreateGroup(groupId).Keys.Add(regKey);
        }

        private async Task<object> LoadInternalAsync<T>(string regKey, object runtimeKey, AssetEntryVO entry)
        {
            try
            {
                var handle = Addressables.LoadAssetAsync<T>(runtimeKey);
                entry.Handle = handle;
                entry.AssetType = typeof(T);

                await handle.Task;

                if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
                {
                    FlowLogger.LogError(SystemLogType.Asset, $"[AssetService] Load failed: {regKey}");
                    _signals.OutGoing.AssetLoadFailed.Dispatch(regKey);
                    _release.HardRelease(regKey);
                    return null;
                }

                entry.Result = handle.Result;
                return entry.Result;
            }
            finally
            {
                _registry.InFlight.Remove(regKey);
            }
        }
    }
}
