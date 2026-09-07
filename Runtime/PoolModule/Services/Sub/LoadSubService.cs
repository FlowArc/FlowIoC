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
                if (itemConfig.IsAddressable && itemConfig is PoolItemCVO preloadAddressable)
                {
                    await _addressableLoadService.PreloadItemAsync(preloadAddressable.AddressablePrefab);
                }

                return;
            }

            for (int i = 0; i < itemConfig.InitialCreateCount; i++)
            {
                IPoolableItem newItem = await CreateItem(itemConfig);
                if (newItem != null)
                {
                    _runtimeModel.AddToPassivePool(newItem, itemKey, group);
                }
            }
        }

        /// <summary>
        /// Builds one item from its config. An addressable prefab is loaded first, which is the one
        /// step here that waits; a direct prefab is built on the spot.
        /// </summary>
        public async Task<IPoolableItem> CreateItem(PoolItemBaseCVO itemConfig)
        {
            if (!TryGetPrefabConfig(itemConfig, out PoolItemCVO poolData))
                return null;

            if (!poolData.IsAddressable)
                return CreateItemSync(itemConfig);

            IPoolableItem poolableItem = await _addressableLoadService.LoadItem(poolData.AddressablePrefab);
            return poolableItem == null ? null : Ready(poolableItem);
        }

        /// <summary>
        /// Builds one item with no Task in the way, for the synchronous Get. Only a direct prefab
        /// can be built this way: an addressable one has to be loaded, and the caller is told to ask
        /// asynchronously instead. The synchronous Get used to read the Task's Result, which holds
        /// only as long as nothing on the way awaits.
        /// </summary>
        public IPoolableItem CreateItemSync(PoolItemBaseCVO itemConfig)
        {
            if (!TryGetPrefabConfig(itemConfig, out PoolItemCVO poolData))
                return null;

            if (poolData.IsAddressable)
            {
                FlowLogger.LogError(SystemLogType.Pool,
                    "[LoadSubService] '" + poolData.PoolKey + "' is addressable and cannot be built synchronously. " +
                    "Use GetAsync, or warm the group first.");
                return null;
            }

            GameObject instance = Object.Instantiate(poolData.Prefab);
            IPoolableItem poolableItem = instance.GetComponent<IPoolableItem>();
            if (poolableItem != null)
                return Ready(poolableItem);

            FlowLogger.LogError(SystemLogType.Pool,
                $"Instantiated prefab does not have an IPoolableItem component. (PoolKey:{itemConfig.PoolKey})");
            Object.Destroy(instance);
            return null;
        }

        private IPoolableItem Ready(IPoolableItem poolableItem)
        {
            poolableItem.ReturnToPoolAction = _returnService.Item;
            poolableItem.OnInitialized();
            return poolableItem;
        }

        private static bool TryGetPrefabConfig(PoolItemBaseCVO itemConfig, out PoolItemCVO poolData)
        {
            poolData = itemConfig as PoolItemCVO;

            if (poolData == null)
            {
                FlowLogger.LogError(SystemLogType.Pool,
                    "[LoadSubService] Item config is a " + (itemConfig == null ? "null" : itemConfig.GetType().Name) +
                    " rather than a PoolItemCVO, so there is no prefab to build from.");
                return false;
            }

            if ((poolData.IsAddressable && poolData.AddressablePrefab == null) || (!poolData.IsAddressable && poolData.Prefab == null))
            {
                FlowLogger.LogError(SystemLogType.Pool,
                    "[LoadSubService] Null prefab in item config for type: " + itemConfig.GetType() + " (PoolKey: " + itemConfig.PoolKey + ")");
                return false;
            }

            return true;
        }
    }
}
