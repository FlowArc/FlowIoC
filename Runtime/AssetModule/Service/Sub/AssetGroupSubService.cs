using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlowIoC.AssetModule.Extensions;
using FlowIoC.AssetModule.Model;
using FlowIoC.AssetModule.Signals;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace FlowIoC.AssetModule.Service.Sub
{
    internal sealed class AssetGroupSubService
    {
        [Inject] private IAssetRegistryModel _registry { get; set; }
        [Inject] private AssetLoadSubService _load { get; set; }
        [InjectSignal] private AssetSignals _signals { get; set; }

        public async Task LoadGroupByLabelAsync<T>(string label, string groupId = null)
        {
            if (string.IsNullOrEmpty(label)) return;
            groupId ??= label;

            var locHandle = Addressables.LoadResourceLocationsAsync(label, typeof(T));
            await locHandle.Task;

            if (!locHandle.IsValid() || locHandle.Result == null || locHandle.Result.Count == 0)
            {
                FlowLogger.LogWarning(SystemLogType.Asset,
                    $"[AssetService] No '{typeof(T).Name}' locations found for label '{label}'. Are the assets labelled?");
                if (locHandle.IsValid()) Addressables.Release(locHandle);
                _registry.GetOrCreateGroup(groupId).IsLoaded = true;
                _signals.OutGoing.GroupLoaded.Dispatch(groupId);
                return;
            }

            var tasks = new List<Task>(locHandle.Result.Count);
            foreach (IResourceLocation loc in locHandle.Result)
                tasks.Add(_load.LoadAssetAsync<T>(loc.PrimaryKey, groupId));

            Addressables.Release(locHandle);
            await Task.WhenAll(tasks);

            _registry.GetOrCreateGroup(groupId).IsLoaded = true;
            _signals.OutGoing.GroupLoaded.Dispatch(groupId);
            FlowLogger.Log(SystemLogType.Asset,
                $"[AssetService] Group loaded by label '{label}' -> '{groupId}' ({tasks.Count} assets)");
        }

        public async Task LoadAssetsAsync<T>(string groupId, IEnumerable<object> keys)
        {
            if (string.IsNullOrEmpty(groupId) || keys == null) return;

            var tasks = new List<Task>();
            foreach (var key in keys)
                tasks.Add(_load.LoadAssetAsync<T>(key, groupId));

            await Task.WhenAll(tasks);

            _registry.GetOrCreateGroup(groupId).IsLoaded = true;
            _signals.OutGoing.GroupLoaded.Dispatch(groupId);
        }

        public void AddToGroup(string groupId, object key)
        {
            if (string.IsNullOrEmpty(groupId)) return;
            if (!AssetKeyExtensions.TryNormalize(key, out var regKey, out _)) return;
            if (!_registry.Entries.TryGetValue(regKey, out var entry)) return;

            if (entry.Owners.Add(groupId))
                _registry.GetOrCreateGroup(groupId).Keys.Add(regKey);
        }

        public bool IsGroupLoaded(string groupId)
            => !string.IsNullOrEmpty(groupId)
               && _registry.Groups.TryGetValue(groupId, out var group)
               && group.IsLoaded;

        public IReadOnlyCollection<string> GetGroupKeys(string groupId)
            => _registry.Groups.TryGetValue(groupId, out var group)
                ? group.Keys
                : Array.Empty<string>();
    }
}
