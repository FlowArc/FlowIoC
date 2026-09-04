#if UNITY_EDITOR
using UnityEngine;

namespace FlowIoC.Editor.ScreenScanner
{
    /// <summary>
    /// Where every column of one Screen Scanner row sits. The column headings and the cells under
    /// them are both placed from this, so a heading stays over its column at any window width
    /// rather than drifting off it the way two separate lists of widths do.
    ///
    /// Load takes whatever is left between the last fixed column and the Reset button, and is
    /// empty when a narrow window leaves nothing.
    /// </summary>
    internal class ScreenColumnsEVO
    {
        private const float NAME_WIDTH = 180f;

        /// <summary>
        /// Narrower than the screen's own name: a Root is named after the module it roots, which
        /// is a word or two, and the column it sits in was wide enough for a sentence.
        /// </summary>
        private const float ROOT_WIDTH = 110f;

        private const float MANAGER_WIDTH = 60f;
        private const float LAYER_WIDTH = 50f;
        private const float TAG_WIDTH = 80f;
        private const float ANIMATION_WIDTH = 68f;
        private const float RESET_WIDTH = 52f;
        private const float GAP = 6f;

        /// <summary>The narrowest Load column still worth drawing text in.</summary>
        private const float LOAD_MINIMUM = 40f;

        internal Rect Name;
        internal Rect Root;
        internal Rect Manager;
        internal Rect Layer;
        internal Rect Tag;
        internal Rect ShowAnimation;
        internal Rect HideAnimation;
        internal Rect Load;
        internal Rect Reset;

        /// <summary>Everything from the name to the last fixed column, for a row that has no cells
        /// to draw and one message to put in their place.</summary>
        internal Rect Message;

        internal ScreenColumnsEVO(Rect row, float contentX)
        {
            float x = row.x + contentX;

            Name = Take(row, ref x, NAME_WIDTH);
            Root = Take(row, ref x, ROOT_WIDTH);
            Manager = Take(row, ref x, MANAGER_WIDTH);
            Layer = Take(row, ref x, LAYER_WIDTH);
            Tag = Take(row, ref x, TAG_WIDTH);
            ShowAnimation = Take(row, ref x, ANIMATION_WIDTH);
            HideAnimation = Take(row, ref x, ANIMATION_WIDTH);

            Reset = new Rect(row.xMax - RESET_WIDTH - GAP, row.y + 2f, RESET_WIDTH, row.height - 4f);

            float load = Mathf.Max(0f, Reset.x - GAP - x);
            Load = new Rect(x, row.y, load < LOAD_MINIMUM ? 0f : load, row.height);

            Message = new Rect(Manager.x, row.y, Mathf.Max(0f, Reset.x - GAP - Manager.x), row.height);
        }

        private Rect Take(Rect row, ref float x, float width)
        {
            var rect = new Rect(x, row.y, width, row.height);
            x += width + GAP;

            return rect;
        }
    }
}
#endif