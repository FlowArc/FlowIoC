using System.Collections.Generic;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Constructables;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.PoolModule.Data.ValueObjects;
using FlowIoC.PoolModule.Entities;
using FlowIoC.PoolModule.RootsContexts;
using UnityEngine;

namespace FlowIoC.PoolModule.Models.Config
{
    internal class PoolConfigModel : IPoolConfigModel, IConstructable
    {
        [Inject(nameof(PoolServiceContext))] private GameObject _root { get; set; }

        [ShowInModelViewer] private readonly Dictionary<string, PoolItemBaseCVO> _itemConfigMap = new(); // itemKey -> itemConfig
        [ShowInModelViewer] private readonly Dictionary<string, string> _itemToGroupConfigMap = new(); // itemKey -> groupConfigKey
        [ShowInModelViewer] private readonly Dictionary<string, PoolGroupCVO> _groupConfigMap = new(); // groupConfigKey -> groupConfig

        public void PostConstruct()
        {
            var adapter = _root.GetComponent<PoolRootAdapter>();
            if (adapter == null) return;

            foreach (var kvp in adapter.PoolGroups)
            {
                var group = kvp.Key;
                var groupConfig = kvp.Value;
                if (groupConfig == null || groupConfig.Group == null)
                {
                    FlowLogger.LogError(SystemLogType.Pool, $"There is a null PoolGroupConfig:({group}) in PoolRootAdapter({_root.name})!!!");
                    continue;
                }

                AddGroupConfig(group, groupConfig);
            }
        }
        
        private void AddGroupConfig(string group, PoolGroupCVO entry)
        {
            if (!_groupConfigMap.ContainsKey(group))
                _groupConfigMap.Add(group, entry);

            foreach (var item in entry.Group.Items)
            {
                var poolKey = entry.GroupSpecificPools ? $"{group}_{item.PoolKey}" : item.PoolKey;
                if (_itemConfigMap.ContainsKey(poolKey))
                {
                    FlowLogger.LogWarning(SystemLogType.Pool, $"Duplicate pool key detected: {poolKey}. Overwriting with latest config.");
                }

                _itemConfigMap[poolKey] = item;
                _itemToGroupConfigMap[poolKey] = group;
            }
        }
        private void RemoveGroupConfig(string group)
        {
            if (!_groupConfigMap.ContainsKey(group)) return;

            foreach (var item in _groupConfigMap[group].Group.Items)
            {
                var poolKey = _groupConfigMap[group].GroupSpecificPools ? $"{group}_{item.PoolKey}" : item.PoolKey;
                _itemConfigMap.Remove(poolKey);
                _itemToGroupConfigMap.Remove(poolKey);
            }

            _groupConfigMap.Remove(group);
        }

        public bool TryGetItemConfig(string itemKey, out PoolItemBaseCVO itemConfig) => _itemConfigMap.TryGetValue(itemKey, out itemConfig);
        public string GetGroupConfigOfItem(string itemKey) => _itemToGroupConfigMap[itemKey];
        public Dictionary<string, PoolGroupCVO> GetGroupConfigMap() => _groupConfigMap;
        public PoolGroupCVO GetGroupConfig(string groupConfigKey) => _groupConfigMap[groupConfigKey];
        public bool IsGroupConfigExist(string groupConfigKey) => _groupConfigMap.ContainsKey(groupConfigKey);
        public void RegisterPoolConfig(KeyValuePair<string, PoolGroupCVO> config) => AddGroupConfig(config.Key, config.Value);
        public void UnregisterPoolConfig(KeyValuePair<string, PoolGroupCVO> config) => RemoveGroupConfig(config.Key);
        public bool IsPostConstructed { get; set; }
        public bool IsDeConstructed { get; set; }

    }
}