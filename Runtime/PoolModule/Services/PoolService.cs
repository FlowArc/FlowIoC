using System;
using FlowIoC.BaseModule.Injectable.Attributes;
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
        public void InitializeGroup(string groupKey)
        {
            if (!Check.IsGroupConfigExist(groupKey)) return;
                
            if (Check.IsGroupCreated(groupKey)) return;
                
            Create.Group(groupKey, _configModel.GetGroupConfig(groupKey));
        }
        public IPoolableItem Get(string itemKey, Transform parent = null, Action<IPoolableItem> callback = null)
        {
            return _getter.Get(itemKey, parent, callback);
        }
        public T Get<T>(string itemKey, Transform parent = null, Action<IPoolableItem> callback = null) where T : class, IPoolableItem
        {
            return _getter.Get<T>(itemKey, parent, callback);
        }
    }
}