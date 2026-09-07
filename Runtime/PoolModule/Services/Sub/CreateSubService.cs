using System.Collections.Generic;
using System.Threading.Tasks;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.PoolModule.Data.ValueObjects;
using FlowIoC.PoolModule.Models.Config;
using FlowIoC.PoolModule.Models.Runtime;

namespace FlowIoC.PoolModule.Services.Sub
{
    public class CreateSubService
    {
        [Inject] private IPoolConfigModel _configModel { get; set; }
        [Inject] private IPoolRuntimeModel _runtimeModel { get; set; }
        [Inject] private LoadSubService _load { get; set; }

        /// <summary>
        /// Registers every pool of the group and fills them. The task ends when the last pool is
        /// full, which for a direct prefab is before this returns and for an addressable one is
        /// after its load. It used to be dropped on the floor, so a fill that failed said nothing.
        /// </summary>
        public Task Group(string groupKey, PoolGroupCVO poolGroupConfig)
        {
            List<Task> fills = new List<Task>(poolGroupConfig.Group.Items.Count);

            foreach (PoolItemCVO item in poolGroupConfig.Group.Items)
            {
                string poolKey = poolGroupConfig.GroupSpecificPools ? $"{groupKey}_{item.PoolKey}" : item.PoolKey;

                _runtimeModel.RegisterPool(poolKey, groupKey);
                fills.Add(_load.Item(item, groupKey, poolKey));
            }

            return Task.WhenAll(fills);
        }
    }
}
