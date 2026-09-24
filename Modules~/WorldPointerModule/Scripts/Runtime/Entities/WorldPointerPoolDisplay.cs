using FlowIoC.ConsoleModule;
using FlowIoC.PoolModule.Entities;
using FlowIoC.PoolModule.Services;
using Modules.WorldPointerModule.Data.ValueObjects;
using Modules.WorldPointerModule.Services;
using UnityEngine;

namespace Modules.WorldPointerModule.Entities
{
    /// <summary>
    /// The ready display: indicators come from the FlowIoC pool under one item key and are
    /// parented under one RectTransform of the screen. A screen's register Command builds one per
    /// channel with the pool it injects and the parent its View holds, and registers it; the
    /// indicator prefab is an item of a pool group like any other pooled object. Acquire is the
    /// pool's synchronous Get, so an addressable indicator has to be in the pool already - warm
    /// its group - or the Get is refused. Not a MonoBehaviour: there is nothing of it in the scene.
    /// </summary>
    public class WorldPointerPoolDisplay<TContent> : IWorldPointerDisplay<TContent>
    {
        private readonly IPoolService _pool;
        private readonly string _itemKey;
        private readonly RectTransform _parent;

        public WorldPointerOptionsCVO Options { get; }

        public WorldPointerPoolDisplay(IPoolService pool, string itemKey, RectTransform parent, WorldPointerOptionsCVO options = null)
        {
            _pool = pool;
            _itemKey = itemKey;
            _parent = parent;
            Options = options;
        }

        public IWorldPointerIndicator<TContent> Acquire()
        {
            IPoolableItem item = _pool.Get(_itemKey, _parent);
            if (item == null) return null;

            if (item is IWorldPointerIndicator<TContent> indicator) return indicator;

            FlowLogger.LogError($"The pool item '{_itemKey}' is {item.GetType().Name}, which does not show {typeof(TContent).Name}. "
                                + $"Its prefab needs an indicator deriving from WorldPointerIndicator<{typeof(TContent).Name}>.",
                item as Object);
            item.Dismiss();
            return null;
        }

        /// <summary>Back to the pool through the item's own Dismiss, which an indicator that fades out overrides.</summary>
        public void Release(IWorldPointerIndicator<TContent> indicator)
        {
            if (indicator is IPoolableItem item) item.Dismiss();
        }

        public override string ToString() =>
            $"pool '{_itemKey}' under {(_parent != null ? _parent.name : "no parent")}";
    }
}
