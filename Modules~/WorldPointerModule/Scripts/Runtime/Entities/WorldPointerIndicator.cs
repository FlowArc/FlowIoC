using FlowIoC.PoolModule.Entities;
using Modules.WorldPointerModule.Enums;
using Modules.WorldPointerModule.Services;
using UnityEngine;

namespace Modules.WorldPointerModule.Entities
{
    /// <summary>
    /// The ordinary indicator's state half: one component on a prefab and no code. Hidden fades to
    /// nothing through the CanvasGroup when there is one and deactivates the GameObject when there
    /// is not; the arrow shows only on the edge. An indicator that shows content derives from
    /// WorldPointerIndicator&lt;TContent&gt;; one that animates a state change overrides SetState.
    /// A pool item, so WorldPointerPoolDisplay hands it out and takes it back; an indicator that
    /// fades out before it goes overrides Dismiss and calls the base at the end of the fade.
    /// Not a View: it holds scene references and reacts, and nothing mediates it.
    /// </summary>
    public class WorldPointerIndicator : PoolableItem, IWorldPointerIndicator
    {
        [SerializeField] private RectTransform _rect;
        [SerializeField] private RectTransform _arrowPivot;
        [SerializeField] private CanvasGroup _canvasGroup;

        private Vector3 _authoredScale = Vector3.one;
        private Quaternion _authoredRotation = Quaternion.identity;

        public RectTransform Rect => _rect != null ? _rect : transform as RectTransform;

        public RectTransform ArrowPivot => _arrowPivot;

        /// <summary>Remembers the prefab's own scale and rotation, before the pool first reparents it.</summary>
        public override void OnInitialized()
        {
            _authoredScale = transform.localScale;
            _authoredRotation = transform.localRotation;
        }

        /// <summary>
        /// The pool reparents keeping world values, so an element coming out from under [Pools] onto a
        /// scaled canvas arrives scaled by the canvas. The prefab's own values are put back.
        /// </summary>
        public override void OnGetFromPool()
        {
            transform.localScale = _authoredScale;
            transform.localRotation = _authoredRotation;
        }

        public virtual void SetState(WorldPointerState state)
        {
            bool visible = state != WorldPointerState.Hidden;

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = visible ? 1f : 0f;
                _canvasGroup.blocksRaycasts = visible;
            }
            else
            {
                gameObject.SetActive(visible);
            }

            if (_arrowPivot != null)
                _arrowPivot.gameObject.SetActive(state == WorldPointerState.OnEdge);
        }
    }

    /// <summary>
    /// An indicator that shows what the world side sends. A game derives one line of class per
    /// content type - EmoteIndicator : WorldPointerIndicator&lt;EmoteVO&gt; - and fills its own
    /// Text or Image in SetContent.
    /// </summary>
    public abstract class WorldPointerIndicator<TContent> : WorldPointerIndicator, IWorldPointerIndicator<TContent>
    {
        public abstract void SetContent(TContent content);
    }
}