using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Constructables;
using FlowIoC.ConsoleModule;
using FlowIoC.PoolModule.Entities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FlowIoC.PoolModule.Models.Runtime
{
    internal class PoolRuntimeModel : IPoolRuntimeModel, IConstructable
    {
        // groupKey -> itemKey -> the active and passive halves of that pool.
        [ShowInModelViewer] private readonly Dictionary<string, Dictionary<string, PoolBucket>> _pools = new();

        private readonly ReferenceComparer _comparer = new();

        private Transform _parent;

        public bool IsPostConstructed { get; set; }
        public bool IsDeconstructed { get; set; }

        public void PostConstruct()
        {
            if (_parent == null)
                _parent = new GameObject("[Pools]").transform;

            Object.DontDestroyOnLoad(_parent.gameObject);
        }

        public void Deconstruct()
        {
            foreach (KeyValuePair<string, Dictionary<string, PoolBucket>> group in _pools)
            {
                foreach (KeyValuePair<string, PoolBucket> pool in group.Value)
                {
                    DestroyAll(pool.Value.Active.Items);
                    DestroyAll(pool.Value.Passive.Items);
                }
            }

            _pools.Clear();

            if (_parent != null)
                Object.Destroy(_parent.gameObject);
        }

        private static void DestroyAll(IReadOnlyList<IPoolableItem> items)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] is Component component && component != null)
                    Object.Destroy(component.gameObject);
            }
        }

        #region groupKey & Pool Registration

        public void RegisterPool(string itemKey, string group)
        {
            if (!_pools.TryGetValue(group, out Dictionary<string, PoolBucket> pools))
            {
                pools = new Dictionary<string, PoolBucket>();
                _pools[group] = pools;
            }

            if (pools.ContainsKey(itemKey))
            {
                FlowLogger.LogWarning(SystemLogType.Pool, $"Pool '{itemKey}' already registered.");
                return;
            }

            pools[itemKey] = new PoolBucket(_comparer);
        }

        public bool IsGroupCreated(string groupKey) => _pools.ContainsKey(groupKey);

        public bool PoolExists(string itemKey, string group) => TryGetBucket(itemKey, group, out _);

        private bool TryGetBucket(string itemKey, string groupKey, out PoolBucket bucket)
        {
            bucket = null;
            return groupKey != null
                   && itemKey != null
                   && _pools.TryGetValue(groupKey, out Dictionary<string, PoolBucket> pools)
                   && pools.TryGetValue(itemKey, out bucket);
        }

        #endregion

        #region Passive / Active Management

        public void AddToPassivePool(IPoolableItem item, string itemKey, string group)
        {
            if (!TryGetBucket(itemKey, group, out PoolBucket bucket)) return;

            if (bucket.Passive.Contains(item))
            {
                FlowLogger.LogWarning(SystemLogType.Pool,
                    $"[PoolRuntimeModel.AddToPassivePool] '{itemKey}' is already in the passive pool. Skipping duplicate return (double-return detected).");
                return;
            }

            if (_parent != null)
                item.transform.SetParent(_parent);

            bucket.Passive.Add(item);
            item.SetActive(false);
        }

        /// <summary>
        /// Hands out one parked item. A cast that does not hold puts the item back rather than
        /// dropping it: it had already left the passive half, and returning false while keeping it
        /// would have lost the instance to both halves for good.
        /// </summary>
        public bool TryGetFromPassivePool<T>(string itemKey, string groupKey, out T item) where T : class, IPoolableItem
        {
            item = null;
            if (!TryGetBucket(itemKey, groupKey, out PoolBucket bucket) || bucket.Passive.Count == 0)
                return false;

            IPoolableItem poolItem = bucket.Passive.TakeLast();
            item = poolItem as T;

            if (item != null)
                return true;

            bucket.Passive.Add(poolItem);
            FlowLogger.LogError(SystemLogType.Pool,
                $"[PoolRuntimeModel.TryGetFromPassivePool] '{itemKey}' is pooled as {poolItem.GetType().Name}, which is not a {typeof(T).Name}.");
            return false;
        }

        /// <summary>
        /// One of the items already in use, a different one on each call so that a caller reaching
        /// for "any of these" does not keep landing on the same instance.
        /// </summary>
        public bool TryGetFromActivePool<T>(string itemKey, string groupKey, out T item) where T : class, IPoolableItem
        {
            item = null;
            if (!TryGetBucket(itemKey, groupKey, out PoolBucket bucket) || bucket.Active.Count == 0)
                return false;

            item = bucket.Active.Rotate() as T;
            return item != null;
        }

        public void AddToActivePool(IPoolableItem item, string itemKey, string groupKey)
        {
            if (!TryGetBucket(itemKey, groupKey, out PoolBucket bucket)) return;
            bucket.Active.Add(item);
        }

        public void RemoveFromActivePool(IPoolableItem item, string itemKey, string groupKey)
        {
            if (!TryGetBucket(itemKey, groupKey, out PoolBucket bucket)) return;
            bucket.Active.Remove(item);
        }

        public void RemoveFromPassivePool(IPoolableItem item, string itemKey, string groupKey)
        {
            if (!TryGetBucket(itemKey, groupKey, out PoolBucket bucket)) return;
            bucket.Passive.Remove(item);
        }

        #endregion

        #region Counts & Queries

        public int GetActiveItemCount(string itemKey, string groupKey)
            => TryGetBucket(itemKey, groupKey, out PoolBucket bucket) ? bucket.Active.Count : 0;

        public int GetPassiveItemCount(string itemKey, string groupKey)
            => TryGetBucket(itemKey, groupKey, out PoolBucket bucket) ? bucket.Passive.Count : 0;

        public IEnumerable<IPoolableItem> GetAllActiveItemsByGroupKey(string groupKey)
        {
            if (!_pools.TryGetValue(groupKey, out Dictionary<string, PoolBucket> pools))
                yield break;

            foreach (KeyValuePair<string, PoolBucket> pool in pools)
            {
                IReadOnlyList<IPoolableItem> items = pool.Value.Active.Items;
                for (int i = 0; i < items.Count; i++)
                    yield return items[i];
            }
        }

        public IEnumerable<IPoolableItem> GetAllPassiveItemsByGroupKey(string groupKey)
        {
            if (!_pools.TryGetValue(groupKey, out Dictionary<string, PoolBucket> pools))
                yield break;

            foreach (KeyValuePair<string, PoolBucket> pool in pools)
            {
                IReadOnlyList<IPoolableItem> items = pool.Value.Passive.Items;
                for (int i = 0; i < items.Count; i++)
                    yield return items[i];
            }
        }

        /// <summary>
        /// A copy, not the pool's own list. Callers of these walk the items and destroy them, and
        /// handing back the live list makes the caller responsible for knowing that a Destroy is
        /// deferred - one day one of them does something that is not, and removes from the
        /// collection it is walking.
        /// </summary>
        public IEnumerable GetAllActiveItemsByItemKey(string itemKey, string groupKey)
            => TryGetBucket(itemKey, groupKey, out PoolBucket bucket)
                ? new List<IPoolableItem>(bucket.Active.Items)
                : Array.Empty<IPoolableItem>();

        public IEnumerable GetAllPassiveItemsByItemKey(string itemKey, string groupKey)
            => TryGetBucket(itemKey, groupKey, out PoolBucket bucket)
                ? new List<IPoolableItem>(bucket.Passive.Items)
                : Array.Empty<IPoolableItem>();

        #endregion

        #region Clear / Unregister

        /// <summary>
        /// Takes one pool out of its group. It used to ask the outer dictionary to remove the item
        /// key, and that dictionary is keyed by group - so this removed nothing, or removed a whole
        /// group that happened to answer to the item's name.
        /// </summary>
        public void UnregisterPool(string itemKey, string groupKey)
        {
            if (!_pools.TryGetValue(groupKey, out Dictionary<string, PoolBucket> pools))
                return;

            pools.Remove(itemKey);
        }

        public void ClearPoolByGroupKey(string groupKey)
        {
            if (!_pools.TryGetValue(groupKey, out Dictionary<string, PoolBucket> pools))
                return;

            foreach (KeyValuePair<string, PoolBucket> pool in pools)
            {
                pool.Value.Active.Clear();
                pool.Value.Passive.Clear();
            }
        }

        public void ClearPoolByItemKey(string itemKey, string groupKey)
        {
            if (!TryGetBucket(itemKey, groupKey, out PoolBucket bucket)) return;

            bucket.Active.Clear();
            bucket.Passive.Clear();
        }

        #endregion

        #region Storage

        private sealed class PoolBucket
        {
            public readonly IndexedItemSet Active;
            public readonly IndexedItemSet Passive;

            public PoolBucket(IEqualityComparer<IPoolableItem> comparer)
            {
                Active = new IndexedItemSet(comparer);
                Passive = new IndexedItemSet(comparer);
            }
        }

        /// <summary>
        /// A set of pooled items that also knows where each one sits, so membership, removal and
        /// checkout are all constant time. A plain list was fine for the small pools the module
        /// was written against, but every return to the pool asked it twice - once to check for a
        /// double return and once to take the item out of the active half - and a pool of five
        /// hundred bullets paid for both scans on every shot.
        /// </summary>
        private sealed class IndexedItemSet
        {
            private readonly List<IPoolableItem> _items = new();
            private readonly Dictionary<IPoolableItem, int> _indices;

            public IndexedItemSet(IEqualityComparer<IPoolableItem> comparer)
            {
                _indices = new Dictionary<IPoolableItem, int>(comparer);
            }

            public int Count => _items.Count;

            public IReadOnlyList<IPoolableItem> Items => _items;

            public bool Contains(IPoolableItem item) => item != null && _indices.ContainsKey(item);

            public void Add(IPoolableItem item)
            {
                if (item == null || _indices.ContainsKey(item))
                    return;

                _indices[item] = _items.Count;
                _items.Add(item);
            }

            /// <summary>
            /// Takes an item out by moving the last one into its place, so nothing behind it has to
            /// shift. The order of a pool is not something anybody reads.
            /// </summary>
            public bool Remove(IPoolableItem item)
            {
                if (item == null || !_indices.Remove(item, out int index))
                    return false;

                int last = _items.Count - 1;

                if (index != last)
                {
                    IPoolableItem moved = _items[last];
                    _items[index] = moved;
                    _indices[moved] = index;
                }

                _items.RemoveAt(last);
                return true;
            }

            public IPoolableItem TakeLast()
            {
                int last = _items.Count - 1;
                IPoolableItem item = _items[last];

                _items.RemoveAt(last);
                _indices.Remove(item);

                return item;
            }

            public IPoolableItem Rotate()
            {
                if (_items.Count == 0)
                    return null;

                IPoolableItem first = _items[0];
                int last = _items.Count - 1;

                if (last > 0)
                {
                    IPoolableItem tail = _items[last];
                    _items[0] = tail;
                    _indices[tail] = 0;
                    _items[last] = first;
                    _indices[first] = last;
                }

                return first;
            }

            public void Clear()
            {
                _items.Clear();
                _indices.Clear();
            }
        }

        /// <summary>
        /// Pooled items are matched by identity. Unity's Object overrides Equals so that a
        /// destroyed object compares equal to null, which is the right answer for scene code and
        /// the wrong one for a lookup table that has to find the entry it stored.
        /// </summary>
        private sealed class ReferenceComparer : IEqualityComparer<IPoolableItem>
        {
            public bool Equals(IPoolableItem x, IPoolableItem y) => ReferenceEquals(x, y);

            public int GetHashCode(IPoolableItem obj) => RuntimeHelpers.GetHashCode(obj);
        }

        #endregion
    }
}