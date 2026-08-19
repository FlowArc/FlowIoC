using System.Collections;
using System.Collections.Generic;
using FlowIoC.PoolModule.Entities;

namespace FlowIoC.PoolModule.Models.Runtime
{
    internal interface IPoolRuntimeModel
    {
        void AddToPassivePool(IPoolableItem item, string itemKey, string group);
        bool TryGetFromPassivePool<T>(string itemKey, string tag, out T item) where T : class, IPoolableItem;
        bool TryGetFromActivePool<T>(string itemKey, string tag, out T item) where T : class, IPoolableItem;
        void AddToActivePool(IPoolableItem item, string itemKey, string tag);
        void RemoveFromActivePool(IPoolableItem item, string itemKey, string tag);
        void RemoveFromPassivePool(IPoolableItem item, string itemKey, string tag);
        
        int GetActiveItemCount(string itemKey, string tag);
        int GetPassiveItemCount(string itemKey, string tag);
        
        //List<IPoolableItem> GetAllActiveItems();
        // List<IPoolableItem> GetAllActiveItemsByTag(string tag);
        IEnumerable<IPoolableItem> GetAllActiveItemsByGroupKey(string tag);
        IEnumerable<IPoolableItem> GetAllPassiveItemsByGroupKey(string tag);

        void RegisterPool(string itemKey, string group);
        void UnregisterPool(string itemKey,string tag);
        bool IsGroupCreated(string tag);
        bool PoolExists(string itemKey, string tag);
        void ClearPoolByGroupKey(string tag);
        IEnumerable GetAllActiveItemsByItemKey(string itemKey, string tag);
        IEnumerable GetAllPassiveItemsByItemKey(string itemKey, string tag);
        void ClearPoolByItemKey(string itemKey, string tag);
    }
} 