using System;
using System.Threading.Tasks;
using FlowIoC.PoolModule.Entities;
using FlowIoC.PoolModule.Services.Sub;
using UnityEngine;

namespace FlowIoC.PoolModule.Services
{
    public interface IPoolService
    {
        void AutoInitializeAll();
        void InitializeAll();
        void InitializeGroup(string groupKey);
        Task InitializeGroupAsync(string groupKey);

        CreateSubService Create { get; }
        ReturnSubService Return { get; }
        CheckSubService Check { get; }
        DestroySubService Destroy { get; }

        IPoolableItem Get(string itemKey, Transform parent = null, Action<IPoolableItem> callback = null);
        T Get<T>(string itemKey, Transform parent = null, Action<IPoolableItem> callback = null) where T : class, IPoolableItem;

        /// <summary>
        /// The asynchronous Get, and the only one an addressable item answers: its prefab may still
        /// have to be loaded, and the synchronous Get cannot wait for that.
        /// </summary>
        Task<IPoolableItem> GetAsync(string itemKey, Transform parent = null, Action<IPoolableItem> callback = null);

        Task<T> GetAsync<T>(string itemKey, Transform parent = null, Action<IPoolableItem> callback = null) where T : class, IPoolableItem;
    }
}
