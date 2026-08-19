using System;
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
        CreateSubService Create { get; }
        ReturnSubService Return { get; }
        CheckSubService Check { get; }
        IPoolableItem Get(string itemKey, Transform parent = null, Action<IPoolableItem> callback = null);
        T Get<T>(string itemKey, Transform parent = null, Action<IPoolableItem> callback = null) where T : class, IPoolableItem;
    }
}