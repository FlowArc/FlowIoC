using System.Collections.Generic;
using System.Threading.Tasks;
using FlowIoC.AssetModule.Data;

namespace FlowIoC.AssetModule.Model
{
    /// <summary>
    /// Every asset loaded through the service, one record per registry key, and every group that
    /// owns some of them. An entry's owners and a group's keys are the two sides of one relation, so
    /// only this Model changes either: a claim or a removal updates both at once.
    /// </summary>
    internal sealed class AssetRegistryModel : IAssetRegistryModel
    {
        private readonly Dictionary<string, AssetEntryVO> _entries = new();
        private readonly Dictionary<string, AssetGroupVO> _groups = new();

        public IReadOnlyDictionary<string, AssetEntryVO> Entries => _entries;
        public IReadOnlyDictionary<string, AssetGroupVO> Groups => _groups;

        public AssetEntryVO GetOrCreateEntry(string registryKey)
        {
            if (!_entries.TryGetValue(registryKey, out var entry))
            {
                entry = new AssetEntryVO();
                _entries[registryKey] = entry;
            }

            return entry;
        }

        public AssetGroupVO GetOrCreateGroup(string groupId)
        {
            if (!_groups.TryGetValue(groupId, out var group))
            {
                group = new AssetGroupVO();
                _groups[groupId] = group;
            }

            return group;
        }

        public void Claim(AssetEntryVO entry, string regKey, string groupId)
        {
            if (groupId == null)
            {
                entry.UnscopedClaims++;
                return;
            }

            if (entry.Owners.Add(groupId))
                GetOrCreateGroup(groupId).Keys.Add(regKey);
        }

        public void Unclaim(AssetEntryVO entry, string regKey, string groupId)
        {
            if (groupId == null)
            {
                if (entry.UnscopedClaims > 0)
                    entry.UnscopedClaims--;

                return;
            }

            if (entry.Owners.Remove(groupId) && _groups.TryGetValue(groupId, out var group))
                group.Keys.Remove(regKey);
        }

        public void RemoveEntry(string regKey)
        {
            if (!_entries.Remove(regKey, out var entry))
                return;

            foreach (var groupId in entry.Owners)
                if (_groups.TryGetValue(groupId, out var group))
                    group.Keys.Remove(regKey);

            entry.Handle?.Release();
        }

        public void RemoveGroup(string groupId) => _groups.Remove(groupId);

        public void MarkGroupLoaded(string groupId) => GetOrCreateGroup(groupId).IsLoaded = true;

        public void SetLoading(AssetEntryVO entry, Task<object> loading) => entry.Loading = loading;

        public void Clear()
        {
            foreach (var entry in _entries.Values)
                entry.Handle?.Release();

            _entries.Clear();
            _groups.Clear();
        }
    }
}