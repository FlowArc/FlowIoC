using System;
using System.Threading.Tasks;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.PoolModule.Entities;
using FlowIoC.PoolModule.Models.Config;
using FlowIoC.PoolModule.Services.Sub;
using FlowIoC.PoolModule.Services.Sub.Getter;
using UnityEngine;

namespace FlowIoC.PoolModule.Services
{
    public class PoolService : IPoolService
    {
        [Inject] private IPoolGetterSubService _getter { get; set; }
        [Inject] private IPoolConfigModel _configModel { get; set; }
        [Inject] public CheckSubService Check { get; private set; }
        [Inject] public CreateSubService Create { get; private set; }
        [Inject] public ReturnSubService Return { get; private set; }
        [Inject] public DestroySubService Destroy { get; private set; }

        public void AutoInitializeAll()
        {
            foreach (var groupKeyValue in _configModel.GetGroupConfigMap())
            {
                if (!groupKeyValue.Value.AutoInitialize) continue;

                InitializeGroup(groupKeyValue.Key);
            }
        }

        public void InitializeAll()
        {
            foreach (var groupKeyValue in _configModel.GetGroupConfigMap())
            {
                InitializeGroup(groupKeyValue.Key);
            }
        }

        /// <summary>
        /// Fills the group and does not wait for it. What went wrong on the way is still reported:
        /// the fill used to be started and forgotten, so an addressable that failed to load left an
        /// empty pool and no word about why.
        /// </summary>
        public void InitializeGroup(string groupKey) => Observe(InitializeGroupAsync(groupKey), groupKey);

        /// <summary>
        /// Fills the group and hands back the fill to wait on. A direct prefab is built before this
        /// returns; an addressable one is loaded first, and this is how a loading screen waits for it.
        /// </summary>
        public Task InitializeGroupAsync(string groupKey)
        {
            if (!Check.IsGroupConfigExist(groupKey))
            {
                FlowLogger.LogWarning(SystemLogType.Pool, $"[PoolService] Group '{groupKey}' has no config, so there is nothing to fill.");
                return Task.CompletedTask;
            }

            if (Check.IsGroupCreated(groupKey))
                return Task.CompletedTask;

            return Create.Group(groupKey, _configModel.GetGroupConfig(groupKey));
        }

        private static async void Observe(Task fill, string groupKey)
        {
            try
            {
                await fill;
            }
            catch (Exception exception)
            {
                FlowLogger.LogError(SystemLogType.Pool,
                    $"[PoolService] Filling group '{groupKey}' stopped: {exception.Message}\n{exception}");
            }
        }

        public IPoolableItem Get(string itemKey, Transform parent = null, Action<IPoolableItem> callback = null)
        {
            return _getter.Get(itemKey, parent, callback);
        }

        public T Get<T>(string itemKey, Transform parent = null, Action<IPoolableItem> callback = null) where T : class, IPoolableItem
        {
            return _getter.Get<T>(itemKey, parent, callback);
        }

        public Task<IPoolableItem> GetAsync(string itemKey, Transform parent = null, Action<IPoolableItem> callback = null)
        {
            return _getter.GetAsync(itemKey, parent, callback);
        }

        public Task<T> GetAsync<T>(string itemKey, Transform parent = null, Action<IPoolableItem> callback = null) where T : class, IPoolableItem
        {
            return _getter.GetAsync<T>(itemKey, parent, callback);
        }
    }
}
