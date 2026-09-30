using System.Collections.Generic;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.PoolModule.Data.ValueObjects;

namespace FlowIoC.PoolModule.Models.Config
{
    /// <summary>
    /// The groups the modules registered, and one record per pool key naming its item's entry and
    /// the group that registered it. Nothing is read at construction: every group arrives from a
    /// PoolSubContext on the Root of the module that uses it.
    /// </summary>
    internal class PoolConfigModel : IPoolConfigModel
    {
        [ShowInModelViewer] private readonly Dictionary<string, PoolItemVO> _items = new(); // poolKey -> item and its group
        [ShowInModelViewer] private readonly Dictionary<string, PoolGroupCVO> _groupConfigMap = new(); // groupConfigKey -> groupConfig

        private void AddGroupConfig(string group, PoolGroupCVO entry)
        {
            if (entry.Group == null)
            {
                FlowLogger.LogError(SystemLogType.Pool, $"[PoolConfigModel.AddGroupConfig][group({group})] has no CD_PoolGroup.");
                return;
            }

            if (!_groupConfigMap.ContainsKey(group))
                _groupConfigMap.Add(group, entry);

            foreach (var item in entry.Group.Items)
            {
                var poolKey = PoolKeyOf(group, entry, item);
                if (_items.ContainsKey(poolKey))
                {
                    FlowLogger.LogWarning(SystemLogType.Pool, $"Duplicate pool key detected: {poolKey}. Overwriting with latest config.");
                }

                _items[poolKey] = new PoolItemVO {Config = item, Group = group};
            }
        }

        /// <summary>
        /// Takes out the group and the pool keys it registered. A key a later group registered over
        /// this one's stays: it is that group's now.
        /// </summary>
        private void RemoveGroupConfig(string group)
        {
            if (!_groupConfigMap.TryGetValue(group, out PoolGroupCVO entry)) return;

            foreach (var item in entry.Group.Items)
            {
                var poolKey = PoolKeyOf(group, entry, item);

                if (_items.TryGetValue(poolKey, out PoolItemVO record) && record.Group == group)
                    _items.Remove(poolKey);
            }

            _groupConfigMap.Remove(group);
        }

        private static string PoolKeyOf(string group, PoolGroupCVO entry, PoolItemBaseCVO item)
            => entry.GroupSpecificPools ? $"{group}_{item.PoolKey}" : item.PoolKey;

        public bool TryGetItemConfig(string itemKey, out PoolItemBaseCVO itemConfig)
        {
            itemConfig = itemKey != null && _items.TryGetValue(itemKey, out PoolItemVO record) ? record.Config : null;
            return itemConfig != null;
        }

        /// <summary>
        /// The group an item was registered under, or null for an item nobody registered. Null
        /// rather than a thrown key: every service asks this first, and a key with a typo in it is
        /// the ordinary way a mistake reaches the pool.
        /// </summary>
        public string GetGroupConfigOfItem(string itemKey)
            => itemKey != null && _items.TryGetValue(itemKey, out PoolItemVO record) ? record.Group : null;

        public IReadOnlyDictionary<string, PoolGroupCVO> GetGroupConfigMap() => _groupConfigMap;
        public PoolGroupCVO GetGroupConfig(string groupConfigKey) => _groupConfigMap[groupConfigKey];
        public bool IsGroupConfigExist(string groupConfigKey) => _groupConfigMap.ContainsKey(groupConfigKey);
        public void RegisterPoolConfig(KeyValuePair<string, PoolGroupCVO> config) => AddGroupConfig(config.Key, config.Value);
        public void UnregisterPoolConfig(KeyValuePair<string, PoolGroupCVO> config) => RemoveGroupConfig(config.Key);
    }
}