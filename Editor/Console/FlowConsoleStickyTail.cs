#if UNITY_EDITOR
using UnityEngine;

namespace FlowIoC.Editor.Console
{
    /// <summary>
    /// Where the log list should sit after its content changed. A view resting at the end follows
    /// new logs down, the way a tail does; a view the reader scrolled up is left alone, because
    /// dragging the list somewhere is a statement that this is where they want to be. Content also
    /// shrinks - a filter, or Collapse folding rows - and then the nearest position that still
    /// exists is the new end, not the top.
    /// </summary>
    public class FlowConsoleStickyTail
    {
        /// <summary>
        /// A scroll position is a float, and a drag or a wheel notch lands fractionally short of
        /// the end. Without this the tail would come off the moment the reader let go.
        /// </summary>
        public const float Tolerance = 2f;

        public float BottomOf(float viewportHeight, float contentHeight)
        {
            return Mathf.Max(0f, contentHeight - viewportHeight);
        }

        public bool IsAtBottom(float scrollY, float viewportHeight, float contentHeight)
        {
            return scrollY >= BottomOf(viewportHeight, contentHeight) - Tolerance;
        }

        public float Follow(bool wasAtBottom, float scrollY, float viewportHeight, float contentHeight)
        {
            float bottom = BottomOf(viewportHeight, contentHeight);

            if (wasAtBottom) return bottom;

            return Mathf.Clamp(scrollY, 0f, bottom);
        }
    }
}
#endif
