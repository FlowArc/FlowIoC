using Modules.WorldPointerModule.Data.UnityObjects;
using Modules.WorldPointerModule.Data.ValueObjects;
using Modules.WorldPointerModule.Enums;
using Modules.WorldPointerModule.Services;
using UnityEngine;

namespace Modules.WorldPointerModule.Entities
{
    /// <summary>
    /// The ordinary indicator: one component on a prefab and no code. Hidden fades to nothing
    /// through the CanvasGroup when there is one and deactivates the GameObject when there is
    /// not; the arrow shows only on the edge. A game that animates a state change writes its own
    /// IWorldPointerIndicator, or derives from this one and overrides SetState. Not a View: it
    /// holds scene references and reacts, and nothing mediates it.
    /// </summary>
    public class WorldPointerIndicator : MonoBehaviour, IWorldPointerIndicator
    {
        [SerializeField] private RectTransform _rect;
        [SerializeField] private RectTransform _arrowPivot;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private CD_WorldPointerOptions _options;

        public RectTransform Rect => _rect != null ? _rect : transform as RectTransform;

        public RectTransform ArrowPivot => _arrowPivot;

        /// <summary>The preset this indicator carries, or null when it carries none - Register then uses the defaults.</summary>
        public WorldPointerOptionsCVO Options => _options != null ? _options.Options : null;

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
}
