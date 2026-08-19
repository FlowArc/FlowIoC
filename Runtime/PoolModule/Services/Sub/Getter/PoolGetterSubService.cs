using System;
using System.Threading.Tasks;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.PoolModule.Data.ValueObjects;
using FlowIoC.PoolModule.Entities;
using FlowIoC.PoolModule.Models.Config;
using FlowIoC.PoolModule.Models.Runtime;
using UnityEngine;

namespace FlowIoC.PoolModule.Services.Sub.Getter
{
    public class PoolGetterSubService : IPoolGetterSubService
    {
        [Inject] private IPoolRuntimeModel _runtimeModel { get; set; }
        [Inject] private IPoolConfigModel _configModel { get; set; }
        [Inject] private LoadSubService _load { get; set; }
        [Inject] private ReturnSubService _return { get; set; }
        
        public IPoolableItem Get(string itemKey, Transform parent = null, Action<IPoolableItem> callback = null)
        {
            return Get<IPoolableItem>(itemKey, parent, callback);
        }
        public T Get<T>(string itemKey, Transform parent = null, Action<IPoolableItem> callback = null)  where T : class, IPoolableItem
        {
            if (string.IsNullOrEmpty(itemKey)) return null;
            
            var groupConfigKey = _configModel.GetGroupConfigOfItem(itemKey);
            if(string.IsNullOrEmpty(groupConfigKey))
            {
                FlowLogger.LogError(SystemLogType.Pool, 
                    $"[PoolGetterSubService.Get] No GroupConfigKey found for itemKey '{itemKey}'. Ensure the item is registered in a pool group.");
                return null;
            }
            
            if (_runtimeModel.TryGetFromPassivePool<T>(itemKey, groupConfigKey, out var item))
            {
                _runtimeModel.AddToActivePool(item, itemKey, groupConfigKey);
                item.ItemKey = itemKey;
                item.ReturnToPoolAction = _return.Item;
                //if (parent != null) 
                item.transform.SetParent(parent);
                item.OnGetFromPool();
                callback?.Invoke(item);
                return item;
            }
            
            if (_configModel.TryGetItemConfig(itemKey, out var itemConfig))
            {
                if (itemConfig is PoolItemVO syncPoolItem && syncPoolItem.IsAddressable)
                {
                    FlowLogger.LogError(SystemLogType.Pool, 
                        $"[PoolGetterSubService.Get] Item '{itemKey}' is Addressable. Use ExecuteAsync instead.");
                    return null;
                }
            
                if (itemConfig.LazyLoad)
                {
                    int createCount = Math.Max(1, itemConfig.InitialCreateCount);
                    IPoolableItem firstCreated = null;
                    for (int i = 0; i < createCount; i++)
                    {
                        var createdItem = _load.CreateItem(itemConfig).Result as T;
                        if (createdItem == null) continue;

                        createdItem.ItemKey = itemKey;
                        createdItem.ReturnToPoolAction = _return.Item;

                        if (i == 0)
                        {
                            firstCreated = createdItem;
                            _runtimeModel.AddToActivePool(createdItem, itemKey, groupConfigKey);
                            if (parent != null) createdItem.transform.SetParent(parent);
                            createdItem.OnGetFromPool();
                        }
                        else
                        {
                            _runtimeModel.AddToPassivePool(createdItem, itemKey, groupConfigKey);
                        }
                    }

                    if (firstCreated != null)
                    {
                        callback?.Invoke(firstCreated);
                        return firstCreated as T;
                    }
                }

                if (itemConfig.IsExtendable)
                {
                    var newItem = _load.CreateItem(itemConfig).Result as T;
                    if (newItem != null)
                    {
                        newItem.ItemKey = itemKey;
                        newItem.ReturnToPoolAction = _return.Item;
                        _runtimeModel.AddToActivePool(newItem, itemKey, groupConfigKey);
                        if (parent != null) newItem.transform.SetParent(parent);
                        newItem.OnGetFromPool();
                        callback?.Invoke(newItem);
                        return newItem;
                    }
                }

                FlowLogger.LogWarning(SystemLogType.Pool, $"[PoolGetterSubService.Get] No available instances for key '{itemKey}'.");
            }

            FlowLogger.LogError(SystemLogType.Pool, $"[PoolGetterSubService.Get] Returning null for itemKey '{itemKey}' tag '{groupConfigKey}'.");
            return default;
        }
        public async Task<IPoolableItem> GetAsync(string itemKey, Transform parent = null, Action<IPoolableItem> callback = null)
        {
            return await GetAsync<IPoolableItem>(itemKey, parent, callback);
        }
        public async Task<T> GetAsync<T>(string itemKey, Transform parent = null, Action<IPoolableItem> callback = null) where T : class, IPoolableItem
        {
            if (string.IsNullOrEmpty(itemKey)) return null;

            var groupConfigKey = _configModel.GetGroupConfigOfItem(itemKey);
            if(string.IsNullOrEmpty(groupConfigKey))
            {
                FlowLogger.LogError(SystemLogType.Pool, 
                    $"[PoolGetterSubService.GetAsync] No GroupConfigKey found for item key '{itemKey}'. Ensure the item is registered in a pool group.");
                return null;
            }

            if (_runtimeModel.TryGetFromPassivePool<T>(itemKey, groupConfigKey, out var item))
            {
                item.ItemKey = itemKey;
                item.ReturnToPoolAction = _return.Item;
                _runtimeModel.AddToActivePool(item, itemKey, groupConfigKey);
                if (parent != null) item.transform.SetParent(parent);
                item.OnGetFromPool();
                callback?.Invoke(item);
                return item;
            }

            if (_configModel.TryGetItemConfig(itemKey, out var itemConfig))
            {
                if (itemConfig.LazyLoad)
                {
                    int createCount = Math.Max(1, itemConfig.InitialCreateCount);
                    IPoolableItem firstCreated = null;
                    for (int i = 0; i < createCount; i++)
                    {
                        var createdItem = await _load.CreateItem(itemConfig) as T;
                        if (createdItem == null) continue;

                        createdItem.ItemKey = itemKey;
                        createdItem.ReturnToPoolAction = _return.Item;

                        if (i == 0)
                        {
                            firstCreated = createdItem;
                            _runtimeModel.AddToActivePool(createdItem, itemKey, groupConfigKey);
                            if (parent != null) createdItem.transform.SetParent(parent);
                            createdItem.OnGetFromPool();
                        }
                        else
                        {
                            _runtimeModel.AddToPassivePool(createdItem, itemKey, groupConfigKey);
                        }
                    }

                    if (firstCreated != null)
                    {
                        callback?.Invoke(firstCreated);
                        return firstCreated as T;
                    }
                }

                if (itemConfig.IsExtendable)
                {
                    var newItem = await _load.CreateItem(itemConfig) as T;
                    if (newItem != null)
                    {
                        newItem.ItemKey = itemKey;
                        newItem.ReturnToPoolAction = _return.Item;
                        _runtimeModel.AddToActivePool(newItem, itemKey, groupConfigKey);
                        if (parent != null) newItem.transform.SetParent(parent);
                        newItem.OnGetFromPool();
                        callback?.Invoke(newItem);
                        return newItem;
                    }
                }

                FlowLogger.LogWarning(SystemLogType.Pool,
                    $"[PoolGetterSubService.GetAsync] No available instances for key '{itemKey}'.");
            }

            FlowLogger.LogError(SystemLogType.Pool,
                $"[PoolGetterSubService.GetAsync] Returning null for itemKey '{itemKey}' tag '{groupConfigKey}'.");
            return default;
        }
    }
}