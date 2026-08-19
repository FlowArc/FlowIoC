using System.Collections;
using System.Collections.Generic;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Constructables;
using FlowIoC.ConsoleModule;
using FlowIoC.PoolModule.Entities;
using UnityEngine;

namespace FlowIoC.PoolModule.Models.Runtime
{
    internal class PoolRuntimeModel : IPoolRuntimeModel, IConstructable
    {
        [ShowInModelViewer] private readonly Dictionary<string, Dictionary<string,LinkedList<IPoolableItem>>> _passiveItems = new();//groupKey -> itemKey -> Ipoolableıtem
        [ShowInModelViewer] private readonly Dictionary<string, Dictionary<string,LinkedList<IPoolableItem>>> _activeItems  = new();//groupKey -> itemKey -> Ipoolableıtem

        private Transform _parent;
        
        public bool IsPostConstructed { get; set; }
        public bool IsDeConstructed { get; set; }

        public void PostConstruct()
        {
            if (_parent == null)
                _parent = new GameObject("[Pools]").transform;
            
            Object.DontDestroyOnLoad(_parent.gameObject);
        }

        public void Deconstruct()
        {
            foreach (var group in _activeItems.Values)
            {
                foreach (var pool in group.Values)
                {
                    foreach (var item in pool)
                    {
                        if (item is Component component && component != null)
                            Object.Destroy(component.gameObject);
                    }
                }
            }

            foreach (var group in _passiveItems.Values)
            {
                foreach (var pool in group.Values)
                {
                    foreach (var item in pool)
                    {
                        if (item is Component component && component != null)
                            Object.Destroy(component.gameObject);
                    }
                }
            }

            _activeItems.Clear();
            _passiveItems.Clear();

            if (_parent != null)
                Object.Destroy(_parent.gameObject);
        }

        #region groupKey & Pool Registration

        public void RegisterPool(string itemKey, string group)
        {
            if (!_passiveItems.ContainsKey(group))
            {
                _passiveItems[group] = new Dictionary<string, LinkedList<IPoolableItem>>();
                _activeItems[group] = new Dictionary<string, LinkedList<IPoolableItem>>();
            }

            if (!_passiveItems[group].ContainsKey(itemKey))
            {
                _passiveItems[group][itemKey] = new LinkedList<IPoolableItem>();
                _activeItems[group][itemKey]  = new LinkedList<IPoolableItem>();
            }
            else
            {
                FlowLogger.LogWarning(SystemLogType.Pool, $"Pool '{itemKey}' already registered.");
            }
        }

        public bool IsGroupCreated(string groupKey) => _passiveItems.ContainsKey(groupKey);
        public bool PoolExists(string itemKey, string group) => _passiveItems.ContainsKey(group) && _passiveItems[group].ContainsKey(itemKey);

        #endregion

        #region Passive / Active Management

        public void AddToPassivePool(IPoolableItem item, string itemKey, string group)
        {
            if (!PoolExists(itemKey, group)) return;

            if (_passiveItems[group][itemKey].Contains(item))
            {
                FlowLogger.LogWarning(SystemLogType.Pool,
                    $"[PoolRuntimeModel.AddToPassivePool] '{itemKey}' is already in the passive pool. Skipping duplicate return (double-return detected).");
                return;
            }

            item.transform.SetParent(_parent);

            _passiveItems[group][itemKey].AddLast(item);
            item.SetActive(false);
        }

        public bool TryGetFromPassivePool<T>(string itemKey, string groupKey, out T item) where T : class, IPoolableItem
        {
            item = null;
            if (!PoolExists(itemKey, groupKey) || _passiveItems[groupKey][itemKey].Count == 0) return false;

            var poolItem = _passiveItems[groupKey][itemKey].First.Value;
            _passiveItems[groupKey][itemKey].Remove(poolItem);
            item = poolItem as T;
            return item != null;
        }

        public bool TryGetFromActivePool<T>(string itemKey, string groupKey, out T item) where T : class, IPoolableItem
        {
            item = default;
            if (!PoolExists(itemKey, groupKey) || _activeItems[groupKey][itemKey].Count == 0) return false;

            var poolItem = _activeItems[groupKey][itemKey].First.Value;
            _activeItems[groupKey][itemKey].RemoveFirst();
            _activeItems[groupKey][itemKey].AddLast(poolItem); // round-robin reuse order
            item = poolItem as T;
            return item != null;
        }

        public void AddToActivePool(IPoolableItem item, string itemKey, string groupKey)
        {
            if (!PoolExists(itemKey, groupKey)) return;
            _activeItems[groupKey][itemKey].AddLast(item);
        }

        public void RemoveFromActivePool(IPoolableItem item, string itemKey, string groupKey)
        {
            if (!PoolExists(itemKey, groupKey)) return;
            _activeItems[groupKey][itemKey].Remove(item);
        }
        
        public void RemoveFromPassivePool(IPoolableItem item, string itemKey, string groupKey)
        {
            if (!PoolExists(itemKey, groupKey)) return;
            _passiveItems[groupKey][itemKey].Remove(item);
        }

        #endregion

        #region Counts & Queries

        public int GetActiveItemCount(string itemKey, string groupKey) => PoolExists(itemKey, groupKey) ? _activeItems[groupKey][itemKey].Count : 0;
        public int GetPassiveItemCount(string itemKey, string groupKey) => PoolExists(itemKey, groupKey) ? _passiveItems[groupKey][itemKey].Count : 0;
        
        // public List<IPoolableItem> GetAllActiveItems() => _activeItems.Values.SelectMany(l => l).ToList();

        // public List<IPoolableItem> GetAllActiveItemsBygroupKey(string groupKey)
        // {
        //     if (!_groupKeyToKeys.ContainsKey(groupKey)) return new List<IPoolableItem>();
        //     var list = new List<IPoolableItem>();
        //     foreach (var key in _groupKeyToKeys[groupKey])
        //     {
        //         list.AddRange(_activeItems[key]);
        //     }
        //     return list;
        // }
        //
        public IEnumerable<IPoolableItem> GetAllActiveItemsByGroupKey(string groupKey)
        {
            foreach (var poolableItems in _activeItems[groupKey].Values)
            {
                foreach (var item in poolableItems)
                    yield return item;
            }
        }
        public IEnumerable<IPoolableItem> GetAllPassiveItemsByGroupKey(string groupKey)
        {
            foreach (var poolableItems in _passiveItems[groupKey].Values)
            {
                foreach (var item in poolableItems)
                    yield return item;
            }
        }
        

        #endregion

        #region Clear / Unregister

        public void UnregisterPool(string itemKey, string groupKey)
        {
            if (!PoolExists(itemKey,groupKey)) return;
            _activeItems.Remove(itemKey);
            _passiveItems.Remove(itemKey);
        }

        public void ClearPoolByGroupKey(string groupKey)
        {
            _activeItems[groupKey].Clear();
            _passiveItems[groupKey].Clear();
            _activeItems.TrimExcess();
            _passiveItems.TrimExcess();
        }

        public IEnumerable GetAllActiveItemsByItemKey(string itemKey, string groupKey)
        {
            if (!PoolExists(itemKey, groupKey)) return new List<IPoolableItem>();
            return _activeItems[groupKey][itemKey];
        }

        public IEnumerable GetAllPassiveItemsByItemKey(string itemKey, string groupKey)
        {
            if (!PoolExists(itemKey, groupKey)) return new List<IPoolableItem>();
            return _passiveItems[groupKey][itemKey];
        }

        public void ClearPoolByItemKey(string itemKey, string groupKey)
        {
            if (!PoolExists(itemKey, groupKey)) return;
            _activeItems[groupKey][itemKey].Clear();
            _passiveItems[groupKey][itemKey].Clear();
        }

        #endregion
    }
} 