using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.PoolModule.Models.Config;
using FlowIoC.PoolModule.Models.Runtime;
using FlowIoC.PoolModule.Services.Sub.Load;
using FlowIoC.PoolModule.Entities;

namespace FlowIoC.PoolModule.Services.Sub
{
    public class CreateSubService
    {
        [Inject] private IPoolConfigModel _configModel { get; set; }
        [Inject] private IPoolRuntimeModel _runtimeModel { get; set; }
        [Inject] private LoadSubService _load { get; set; }

        public void Group(string groupKey, PoolGroupCVO poolGroupConfig)
        {
            foreach (var item in poolGroupConfig.Group.Items)
            {
                var poolKey = poolGroupConfig.GroupSpecificPools ? $"{groupKey}_{item.PoolKey}" : item.PoolKey;
                
                _runtimeModel.RegisterPool(poolKey, groupKey);
                _load.Item(item, groupKey, poolKey).ConfigureAwait(false);
            }
        }
    }
} 