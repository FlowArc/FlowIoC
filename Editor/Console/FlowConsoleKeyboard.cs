#if UNITY_EDITOR
using UnityEngine;

namespace FlowIoC.Editor.Console
{
    /// <summary>
    /// Walking the log list from the keyboard. It answers two questions and holds no state: which
    /// row a key press selects, and how far the view has to move for that row to be on screen.
    /// Enter and copy are not here - those act on the selected log rather than choose one, so the
    /// window does them where it has the log in hand.
    /// </summary>
    public class FlowConsoleKeyboard
    {
        /// <summary>
        /// The row <paramref name="key"/> selects, or false when the key is not one this walks
        /// with - which is the window's cue to leave the event alone.
        /// </summary>
        public bool TryMove(KeyCode key, int index, int count, int rowsPerPage, out int moved)
        {
            moved = index;

            if (count <= 0) return false;

            int page = Mathf.Max(1, rowsPerPage);
            int last = count - 1;

            switch (key)
            {
                // Nothing is selected until something is clicked, so the first press chooses for
                // itself: down starts at the top, up starts at the end.
                case KeyCode.DownArrow:
                    moved = index < 0 ? 0 : index + 1;
                    break;

                case KeyCode.UpArrow:
                    moved = index < 0 ? last : index - 1;
                    break;

                case KeyCode.PageDown:
                    moved = index < 0 ? 0 : index + page;
                    break;

                case KeyCode.PageUp:
                    moved = index < 0 ? last : index - page;
                    break;

                case KeyCode.Home:
                    moved = 0;
                    break;

                case KeyCode.End:
                    moved = last;
                    break;

                default:
                    return false;
            }

            moved = Mathf.Clamp(moved, 0, last);
            return true;
        }

        /// <summary>
        /// The smallest scroll that puts the row on screen. Arrowing off the bottom edge moves the
        /// list by one row rather than throwing the row into the middle of the view - a list that
        /// jumps under the reader cannot be walked.
        /// </summary>
        public float Reveal(float scrollY, float viewportHeight, float rowTop, float rowHeight)
        {
            if (rowTop < scrollY) return Mathf.Max(0f, rowTop);

            float rowBottom = rowTop + rowHeight;
            if (rowBottom > scrollY + viewportHeight) return Mathf.Max(0f, rowBottom - viewportHeight);

            return scrollY;
        }
    }
}
#endif
