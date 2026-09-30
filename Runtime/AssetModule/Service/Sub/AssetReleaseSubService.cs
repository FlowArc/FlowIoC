using System.Collections.Generic;
using FlowIoC.AssetModule.Extensions;
using FlowIoC.AssetModule.Model;
using FlowIoC.AssetModule.Signals;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;

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

            _registry.Unclaim(entry, regKey, groupId);

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

                _registry.Unclaim(entry, regKey, groupId);
                if (!entry.HasOwners) HardRelease(regKey);
            }

            _registry.RemoveGroup(groupId);
            _signals.Outgoing.GroupReleased.Dispatch(groupId);
            FlowLogger.Log(SystemLogType.Asset, $"[AssetReleaseSubService.ReleaseGroup][groupId({groupId})]");
        }

        public void HardRelease(string regKey) => _registry.RemoveEntry(regKey);
    }
}
