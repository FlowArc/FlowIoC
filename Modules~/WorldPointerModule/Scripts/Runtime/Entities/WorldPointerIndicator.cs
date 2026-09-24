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
    /// Not a View: it holds scene references and reacts, and nothing mediates it.
    /// </summary>
    public class WorldPointerIndicator : MonoBehaviour, IWorldPointerIndicator
    {
        [SerializeField] private RectTransform _rect;
        [SerializeField] private RectTransform _arrowPivot;
        [SerializeField] private CanvasGroup _canvasGroup;

        public RectTransform Rect => _rect != null ? _rect : transform as RectTransform;

        public RectTransform ArrowPivot => _arrowPivot;

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