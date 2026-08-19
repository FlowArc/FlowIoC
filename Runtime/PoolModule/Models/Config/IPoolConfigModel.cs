using System.Collections.Generic;
using FlowIoC.PoolModule.Data.ValueObjects;
using FlowIoC.PoolModule.Entities;

namespace FlowIoC.PoolModule.Models.Config
{
    internal interface IPoolConfigModel
    {
        bool TryGetItemConfig(string itemKey, out PoolItemBaseCVO itemConfig);
        string GetGroupConfigOfItem(string itemKey);
        Dictionary<string, PoolGroupCVO> GetGroupConfigMap();
        PoolGroupCVO GetGroupConfig(string groupKey);
        bool IsGroupConfigExist(string tag);
        void RegisterPoolConfig(KeyValuePair<string, PoolGroupCVO> config);
        void UnregisterPoolConfig(KeyValuePair<string, PoolGroupCVO> config);
    }
}