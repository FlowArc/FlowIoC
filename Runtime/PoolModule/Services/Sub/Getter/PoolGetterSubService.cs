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

        public T Get<T>(string itemKey, Transform parent = null, Action<IPoolableItem> callback = null) where T : class, IPoolableItem
        {
            if (!TryStart(itemKey, "Get", out string groupConfigKey))
                return null;

            if (_runtimeModel.TryGetFromPassivePool(itemKey, groupConfigKey, out T pooled))
                return CheckOut(pooled, itemKey, groupConfigKey, parent, callback);

            if (!_configModel.TryGetItemConfig(itemKey, out PoolItemBaseCVO itemConfig))
                return ReportEmpty<T>(itemKey, groupConfigKey, "Get");

            if (itemConfig is PoolItemCVO addressableItem && addressableItem.IsAddressable)
            {
                FlowLogger.LogError(SystemLogType.Pool,
                    $"[PoolGetterSubService.Get] Item '{itemKey}' is Addressable. Use GetAsync instead.");
                return null;
            }

            if (itemConfig.LazyLoad)
            {
                T created = FillLazily(itemKey, groupConfigKey, itemConfig, () => _load.CreateItemSync(itemConfig) as T);
                if (created != null)
                    return CheckOut(created, itemKey, groupConfigKey, parent, callback);
            }

            if (itemConfig.IsExtendable)
            {
                T extended = _load.CreateItemSync(itemConfig) as T;
                if (extended != null)
                    return CheckOut(extended, itemKey, groupConfigKey, parent, callback);
            }

            return ReportEmpty<T>(itemKey, groupConfigKey, "Get");
        }

        public async Task<IPoolableItem> GetAsync(string itemKey, Transform parent = null, Action<IPoolableItem> callback = null)
        {
            return await GetAsync<IPoolableItem>(itemKey, parent, callback);
        }

        public async Task<T> GetAsync<T>(string itemKey, Transform parent = null, Action<IPoolableItem> callback = null)
            where T : class, IPoolableItem
        {
            if (!TryStart(itemKey, "GetAsync", out string groupConfigKey))
                return null;

            if (_runtimeModel.TryGetFromPassivePool(itemKey, groupConfigKey, out T pooled))
                return CheckOut(pooled, itemKey, groupConfigKey, parent, callback);

            if (!_configModel.TryGetItemConfig(itemKey, out PoolItemBaseCVO itemConfig))
                return ReportEmpty<T>(itemKey, groupConfigKey, "GetAsync");

            if (itemConfig.LazyLoad)
            {
                T created = await FillLazilyAsync(itemKey, groupConfigKey, itemConfig,
                    async () => await _load.CreateItem(itemConfig) as T);

                if (created != null)
                    return CheckOut(created, itemKey, groupConfigKey, parent, callback);
            }

            if (itemConfig.IsExtendable)
            {
                T extended = await _load.CreateItem(itemConfig) as T;
                if (extended != null)
                    return CheckOut(extended, itemKey, groupConfigKey, parent, callback);
            }

            return ReportEmpty<T>(itemKey, groupConfigKey, "GetAsync");
        }

        #region Shared steps

        private bool TryStart(string itemKey, string caller, out string groupConfigKey)
        {
            groupConfigKey = null;

            if (string.IsNullOrEmpty(itemKey))
                return false;

            groupConfigKey = _configModel.GetGroupConfigOfItem(itemKey);

            if (!string.IsNullOrEmpty(groupConfigKey))
                return true;

            FlowLogger.LogError(SystemLogType.Pool,
                $"[PoolGetterSubService.{caller}] No GroupConfigKey found for itemKey '{itemKey}'. Ensure the item is registered in a pool group.");
            return false;
        }

        /// <summary>
        /// Everything a pool does as an item leaves it, in one place so that an item handed back
        /// from the passive half and one built on the spot arrive in the same state. Activation is
        /// part of that: an item is deactivated on its way into the pool, so the pool is what turns
        /// it back on, and a caller no longer has to know whether the instance is new or recycled.
        /// The reparent is unconditional, because a recycled item is sitting under [Pools] and a
        /// null parent means the scene root rather than "leave it where it is".
        /// </summary>
        private T CheckOut<T>(T item, string itemKey, string groupConfigKey, Transform parent, Action<IPoolableItem> callback)
            where T : class, IPoolableItem
        {
            item.ItemKey = itemKey;
            item.ReturnToPoolAction = _return.Item;

            _runtimeModel.AddToActivePool(item, itemKey, groupConfigKey);

            item.transform.SetParent(parent);
            item.SetActive(true);
            item.OnGetFromPool();

            callback?.Invoke(item);
            return item;
        }

        /// <summary>
        /// Builds the item's initial batch. The first one is handed to the caller and the rest are
        /// parked, so the next Get for this key is answered from the pool.
        /// </summary>
        private T FillLazily<T>(string itemKey, string groupConfigKey, PoolItemBaseCVO itemConfig, Func<T> create)
            where T : class, IPoolableItem
        {
            int createCount = Math.Max(1, itemConfig.InitialCreateCount);
            T first = null;

            for (int i = 0; i < createCount; i++)
                first = Adopt(create(), itemKey, groupConfigKey, first);

            return first;
        }

        /// <summary>
        /// The same batch, awaited. The asynchronous Get used to write this loop out again inline,
        /// which is how the two drifted apart: only the create call was ever different.
        /// </summary>
        private async Task<T> FillLazilyAsync<T>(string itemKey, string groupConfigKey, PoolItemBaseCVO itemConfig,
            Func<Task<T>> create)
            where T : class, IPoolableItem
        {
            int createCount = Math.Max(1, itemConfig.InitialCreateCount);
            T first = null;

            for (int i = 0; i < createCount; i++)
                first = Adopt(await create(), itemKey, groupConfigKey, first);

            return first;
        }

        /// <summary>
        /// One item out of the batch: named, wired to return itself, and then either kept back as
        /// the one the caller gets or parked in the passive pool. This is the whole of what the two
        /// fills have in common.
        /// </summary>
        private T Adopt<T>(T created, string itemKey, string groupConfigKey, T first)
            where T : class, IPoolableItem
        {
            if (created == null)
                return first;

            created.ItemKey = itemKey;
            created.ReturnToPoolAction = _return.Item;

            if (first == null)
                return created;

            _runtimeModel.AddToPassivePool(created, itemKey, groupConfigKey);
            return first;
        }

        private T ReportEmpty<T>(string itemKey, string groupConfigKey, string caller) where T : class, IPoolableItem
        {
            FlowLogger.LogError(SystemLogType.Pool,
                $"[PoolGetterSubService.{caller}] Returning null for itemKey '{itemKey}' group '{groupConfigKey}'.");
            return null;
        }

        #endregion
    }
}