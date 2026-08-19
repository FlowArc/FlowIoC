using System.Linq;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.PoolModule.Entities;
using FlowIoC.PoolModule.Models.Config;
using FlowIoC.PoolModule.Models.Runtime;

namespace FlowIoC.PoolModule.Services.Sub
{
    public class ReturnSubService
    {
        [Inject] private IPoolRuntimeModel _runtimeModel { get; set; }
        [Inject] private IPoolConfigModel _configModel { get; set; }

        public void Item(IPoolableItem item)
        {
            if (item == null)
            {
                FlowLogger.LogError(SystemLogType.Pool, "Cannot return null item to pool.");
                return;
            }

            var key = item.ItemKey;
            var groupKey = _configModel.GetGroupConfigOfItem(key);

            _runtimeModel.RemoveFromActivePool(item, key, groupKey);
            _runtimeModel.AddToPassivePool(item, key, groupKey);
            item.OnReturnToPool();
        }
        
        public void Group(string groupKey)
        {
            if (string.IsNullOrEmpty(groupKey))
            {
                FlowLogger.LogError(SystemLogType.Pool, "Cannot return items to pool with null or empty groupKey.");
                return;
            }

            // Materialize first because Item(...) mutates the LinkedList that
            // GetAllActiveItemsByGroupKey enumerates via yield-return.
            var items = _runtimeModel.GetAllActiveItemsByGroupKey(groupKey).ToList();
            foreach (var item in items)
            {
                Item(item);
            }
        }
    }
} 