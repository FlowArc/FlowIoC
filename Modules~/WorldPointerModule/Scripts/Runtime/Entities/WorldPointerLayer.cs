using System.Collections.Generic;
using FlowIoC.ConsoleModule;
using Modules.WorldPointerModule.Data.UnityObjects;
using Modules.WorldPointerModule.Data.ValueObjects;
using Modules.WorldPointerModule.Services;
using UnityEngine;

namespace Modules.WorldPointerModule.Entities
{
    /// <summary>
    /// The ready display: a component in a screen's prefab that hands out indicators from a prefab
    /// and keeps the ones it gets back. A game derives one line of class per content type -
    /// EmoteLayer : WorldPointerLayer&lt;EmoteVO&gt; - and the screen's opening Command registers it
    /// under its id. Released indicators are switched off and kept; Release is virtual, so a
    /// display that fades an indicator out first overrides it and calls ReturnToPool at the end.
    /// Not a View: the screen's View holds it, and nothing mediates it.
    /// </summary>
    public abstract class WorldPointerLayer<TContent> : MonoBehaviour, IWorldPointerDisplay<TContent>
    {
        [SerializeField] private WorldPointerIndicator _prefab;
        [SerializeField] private RectTransform _parent;
        [SerializeField] private CD_WorldPointerOptions _options;

        private readonly Stack<WorldPointerIndicator> _pool = new();

        public WorldPointerOptionsCVO Options => _options != null ? _options.Options : null;

        /// <summary>Where indicators are parented: the named RectTransform, or this component's own.</summary>
        public RectTransform Parent => _parent != null ? _parent : transform as RectTransform;

        public IWorldPointerIndicator<TContent> Acquire()
        {
            WorldPointerIndicator indicator = _pool.Count > 0 ? _pool.Pop() : Create();
            if (indicator == null) return null;

            indicator.gameObject.SetActive(true);
            return (IWorldPointerIndicator<TContent>) indicator;
        }

        public virtual void Release(IWorldPointerIndicator<TContent> indicator) => ReturnToPool(indicator);

        /// <summary>Switches the indicator off and keeps it for the next Acquire.</summary>
        protected void ReturnToPool(IWorldPointerIndicator<TContent> indicator)
        {
            if (indicator is not WorldPointerIndicator component || component == null) return;

            component.gameObject.SetActive(false);
            _pool.Push(component);
        }

        private WorldPointerIndicator Create()
        {
            if (_prefab == null || _prefab is not IWorldPointerIndicator<TContent>)
            {
                string found = _prefab == null ? "no prefab" : _prefab.GetType().Name;
                FlowLogger.LogError($"{GetType().Name} on {name} needs a prefab whose indicator shows {typeof(TContent).Name}, "
                                    + $"and has {found}. Give it one deriving from WorldPointerIndicator<{typeof(TContent).Name}>.",
                    this);
                return null;
            }

            WorldPointerIndicator indicator = Instantiate(_prefab, Parent, false);
            indicator.name = _prefab.name;
            return indicator;
        }
    }
}
