using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.PoolModule.Models.Config;
using FlowIoC.PoolModule.Models.Runtime;
using FlowIoC.PoolModule.Services.Sub.Load;
using FlowIoC.PoolModule.Data.ValueObjects;
using FlowIoC.PoolModule.Entities;
using UnityEngine;

namespace FlowIoC.PoolModule.Services.Sub
{
    public class DestroySubService
    {
        [Inject] private IPoolConfigModel _configModel { get; set; }
        [Inject] private IPoolRuntimeModel _runtimeModel { get; set; }
        [Inject] private AddressableLoadSubService _addressableLoadService { get; set; }

        public void Group(string groupKey)
        {
            foreach (var item in _runtimeModel.GetAllActiveItemsByGroupKey(groupKey))
                Object.Destroy(item.transform.gameObject);
            
            foreach (var item in _runtimeModel.GetAllPassiveItemsByGroupKey(groupKey))
                Object.Destroy(item.transform.gameObject);

            foreach (var item in _configModel.GetGroupConfigMap()[groupKey].Group.Items)
            {
                if (item is PoolItemVO poolItem && poolItem.IsAddressable)
                {
                    _addressableLoadService.UnloadItem(poolItem.AddressablePrefab);
                }
            }
            
            _runtimeModel.ClearPoolByGroupKey(groupKey);
        }
        
        public void Pool(string itemKey)
        {
            if (!_configModel.TryGetItemConfig(itemKey, out var itemConfig))
            {
                FlowLogger.LogError(SystemLogType.Pool, $"Lifecycle: Cannot destroy pool. Item config '{itemKey}' not found.");
                return;
            }
            
            var tag = _configModel.GetGroupConfigOfItem(itemKey);
            if (!_runtimeModel.PoolExists(itemKey, tag))
            {
                FlowLogger.LogWarning(SystemLogType.Pool, $"Lifecycle: Pool '{itemKey}' does not exist.");
                return;
            }
            
            foreach (IPoolableItem item in _runtimeModel.GetAllActiveItemsByItemKey(itemKey, tag))
                Object.Destroy(item.transform.gameObject);
            
            foreach (IPoolableItem item in _runtimeModel.GetAllPassiveItemsByItemKey(itemKey, tag))
                Object.Destroy(item.transform.gameObject);
            
            if (itemConfig is PoolItemVO addressableCfg && addressableCfg.IsAddressable)
            {
                _addressableLoadService.UnloadItem(addressableCfg.AddressablePrefab);
            }

            _runtimeModel.ClearPoolByItemKey(itemKey, tag);
        }
        
        public void Item(IPoolableItem item)
        {
            if (item == null)
            {
                FlowLogger.LogError(SystemLogType.Pool, "Cannot destroy null item.");
                return;
            }

            var key = item.ItemKey;
            var tag = _configModel.GetGroupConfigOfItem(key);

            _runtimeModel.RemoveFromActivePool(item, key, tag);
            _runtimeModel.RemoveFromPassivePool(item, key, tag);
            Object.Destroy(item.transform.gameObject);
        }
    }
} 