using System.Collections.Generic;
using FlowIoC.AssetModule.Extensions;
using FlowIoC.AssetModule.Model;
using FlowIoC.AssetModule.Signals;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using UnityEngine.AddressableAssets;

namespace FlowIoC.AssetModule.Service.Sub
{
    internal sealed class AssetReleaseSubService
    {
        [Inject] private IAssetRegistryModel _registry { get; set; }
        [InjectSignal] private AssetSignals _signals { get; set; }

        public void Release(object key, string groupId = null)
        {
            if (!AssetKeyExtensions.TryNormalize(key, out var regKey, out _)) return;
            if (!_registry.Entries.TryGetValue(regKey, out var entry)) return;

            if (groupId != null)
            {
                if (entry.Owners.Remove(groupId) && _registry.Groups.TryGetValue(groupId, out var group))
                    group.Keys.Remove(regKey);
            }
            else if (entry.UnscopedClaims > 0)
            {
                entry.UnscopedClaims--;
            }

            if (!entry.HasOwners) HardRelease(regKey);
        }

        public void ReleaseGroup(string groupId)
        {
            if (string.IsNullOrEmpty(groupId)) return;
            if (!_registry.Groups.TryGetValue(groupId, out var group)) return;

            var keys = new List<string>(group.Keys);
            foreach (var regKey in keys)
            {
                if (!_registry.Entries.TryGetValue(regKey, out var entry)) continue;

                entry.Owners.Remove(groupId);
                if (!entry.HasOwners) HardRelease(regKey);
            }

            _registry.Groups.Remove(groupId);
            _signals.OutGoing.GroupReleased.Dispatch(groupId);
            FlowLogger.Log(SystemLogType.Asset, $"[AssetService] Group released: {groupId}");
        }

        public void HardRelease(string regKey)
        {
            if (!_registry.Entries.TryGetValue(regKey, out var entry)) return;

            foreach (var groupId in entry.Owners)
                if (_registry.Groups.TryGetValue(groupId, out var group))
                    group.Keys.Remove(regKey);

            if (entry.Handle.IsValid())
                Addressables.Release(entry.Handle);

            _registry.Entries.Remove(regKey);
        }
    }
}
