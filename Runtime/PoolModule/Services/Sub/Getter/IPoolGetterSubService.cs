using System;
using System.Threading.Tasks;
using FlowIoC.PoolModule.Entities;
using UnityEngine;

namespace FlowIoC.PoolModule.Services.Sub.Getter
{
    public interface IPoolGetterSubService
    {
        IPoolableItem Get(string itemKey, Transform parent = null, Action<IPoolableItem> callback = null);
        T Get<T>(string itemKey, Transform parent = null, Action<IPoolableItem> callback = null) where T : class, IPoolableItem;
        Task<IPoolableItem> GetAsync(string itemKey, Transform parent = null, Action<IPoolableItem> callback = null);
        Task<T> GetAsync<T>(string itemKey, Transform parent = null, Action<IPoolableItem> callback = null) where T : class, IPoolableItem;
    }
} 