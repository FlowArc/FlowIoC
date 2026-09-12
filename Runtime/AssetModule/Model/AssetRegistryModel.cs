using System.Collections.Generic;
using System.Threading.Tasks;
using FlowIoC.AssetModule.Data;

namespace FlowIoC.AssetModule.Model
{
    internal sealed class AssetRegistryModel : IAssetRegistryModel
    {
        public Dictionary<string, AssetEntryVO> Entries { get; } = new();
        public Dictionary<string, AssetGroupVO> Groups { get; } = new();
        public Dictionary<string, Task<object>> InFlight { get; } = new();

        public AssetEntryVO GetOrCreateEntry(string registryKey)
        {
            if (!Entries.TryGetValue(registryKey, out var entry))
            {
                entry = new AssetEntryVO();
                Entries[registryKey] = entry;
            }

            return entry;
        }

        public AssetGroupVO GetOrCreateGroup(string groupId)
        {
            if (!Groups.TryGetValue(groupId, out var group))
            {
                group = new AssetGroupVO();
                Groups[groupId] = group;
            }

            return group;
        }

        public void Clear()
        {
            foreach (var entry in Entries.Values)
                entry.Handle?.Release();

            Entries.Clear();
            Groups.Clear();
            InFlight.Clear();
        }
    }
}
