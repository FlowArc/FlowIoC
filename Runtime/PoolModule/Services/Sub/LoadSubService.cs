using System.Threading.Tasks;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.PoolModule.Data.ValueObjects;
using FlowIoC.PoolModule.Entities;
using FlowIoC.PoolModule.Models.Runtime;
using FlowIoC.PoolModule.Services.Sub.Load;
using UnityEngine;

namespace FlowIoC.PoolModule.Services.Sub
{
    public class LoadSubService
    {
        [Inject] private IPoolRuntimeModel _runtimeModel { get; set; }
        [Inject] private AddressableLoadSubService _addressableLoadService { get; set; }
        [Inject] private ReturnSubService _returnService { get; set; }

        public async Task Item(PoolItemBaseCVO itemConfig, string group, string itemKey)
        {
            if (itemConfig.LazyLoad)
            {
                if (itemConfig.IsAddressable && itemConfig is PoolItemVO preloadAddressable)
                {
                    await _addressableLoadService.PreloadItemAsync(preloadAddressable.AddressablePrefab);
                }
                return;
            }

            for (int i = 0; i < itemConfig.InitialCreateCount; i++)
            {
                var newItem = await CreateItem(itemConfig);
                if (newItem != null)
                {
                    _runtimeModel.AddToPassivePool(newItem, itemKey, group);
                }
            }
        }

        public async Task<IPoolableItem> CreateItem(PoolItemBaseCVO itemConfig)
        {
            IPoolableItem poolableItem = null;

            var poolData = itemConfig as PoolItemVO;
            if ((poolData.IsAddressable && poolData.AddressablePrefab == null) || (!poolData.IsAddressable && poolData.Prefab == null))
            {
                FlowLogger.LogError(SystemLogType.Pool, "[LoadSubService] Null prefab in item config for type: " + itemConfig.GetType() + " (PoolKey: " + itemConfig.PoolKey + ")");
                return null;
            }

            if (poolData.IsAddressable)
            {
                poolableItem = await _addressableLoadService.LoadItem(poolData.AddressablePrefab);
            }
            else
            {
                var prefab = poolData.Prefab;
                if (prefab != null)
                {
                    var instance = Object.Instantiate(prefab);
                    poolableItem = instance.GetComponent<IPoolableItem>();
                    if (poolableItem == null)
                    {
                        FlowLogger.LogError(SystemLogType.Pool, $"Instantiated prefab does not have an IPoolableItem component. (PoolKey:{itemConfig.PoolKey})");
                        Object.Destroy(instance);
                    }
                }
            }

            if (poolableItem != null)
            {
                poolableItem.ReturnToPoolAction = _returnService.Item;
                poolableItem.OnInitialized();
                return poolableItem;
            }
            
            return null;
        }
    }
} 