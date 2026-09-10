#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEngine;

namespace FlowIoC.Editor.Inspector
{
    /// <summary>
    /// The tree a FlowIoC window draws its rows as: a row stepped in by how deep it sits, and a
    /// guide line from the row it lives in down to it. Module Scanner, the parent picker of the
    /// generators, Delete Module and the folder preview all draw the same tree with this, so a
    /// reader who has learned to read one of them has learned them all.
    ///
    /// The rows themselves are FlowRowPainter's. This only knows where each was drawn, so that
    /// a nested row can hang from its parent's.
    /// </summary>
    public class FlowTreePainter
    {
        /// <summary>
        /// How far a row steps in for each level it sits inside: one slot, so a nested row's
        /// first glyph sits under the second glyph of the row it lives in and the guide line
        /// between them has one slot's room to hang in.
        /// </summary>
        public const float INDENT_WIDTH = SLOT_WIDTH;

        /// <summary>
        /// The width of a row's first cell - the foldout arrow, in a tree whose rows fold - which
        /// is also the column a guide line hangs from the middle of. A tree whose rows have no
        /// first cell still hangs its guides from the same place, which puts the line under the
        /// first letters of the parent's name.
        /// </summary>
        public const float SLOT_WIDTH = 16f;

        /// <summary>The air between a guide line's stub and what it points at.</summary>
        private const float GUIDE_GAP = 2f;

        private readonly FlowRowPainter _rows;

        /// <summary>
        /// What every row carries before the tree starts: the column a pick mark or a checkbox
        /// sits in, at the row's edge rather than at the row's depth. A mark that moved in with
        /// the indent left an empty slot between the guide line and the name on every row that
        /// carried no mark, and the slot read as a gap.
        /// </summary>
        private readonly float _lead;

        /// <summary>
        /// Where each row was drawn this pass, so a nested row can hang its guide line from the
        /// row it lives in. A parent is always drawn before its children, so by the time a child
        /// asks, the rect is there - unless a filter hid the parent, in which case the child was
        /// hidden with it.
        /// </summary>
        private readonly Dictionary<object, Rect> _rects = new Dictionary<object, Rect>();

        public FlowTreePainter(FlowRowPainter rows, float lead = 0f)
        {
            _rows = rows;
            _lead = lead;
        }

        /// <summary>Forgets the last pass's rows. Once per OnGUI, before the first row.</summary>
        public void Begin()
        {
            _rects.Clear();
        }

        public float Indent(int depth) => depth * INDENT_WIDTH;

        /// <summary>Where the column before the tree starts, on every row alike.</summary>
        public float LeadX(Rect rect) => rect.x + _rows.ContentX - 1f;

        /// <summary>
        /// Where a row's first cell starts: past the stripe and the lead, stepped in by its depth.
        /// </summary>
        public float SlotX(Rect rect, int depth) => rect.x + _rows.ContentX + _lead - 1f + Indent(depth);

        /// <summary>
        /// Where a row's text starts when the row has no first cell - no arrow to fold it - so the
        /// name sits right after the guide line's stub rather than a slot's width past it.
        /// </summary>
        public float TextX(Rect rect, int depth) => SlotX(rect, depth) + 1f;

        /// <summary>
        /// Records where a row was drawn and hangs it from its parent's row when that was drawn
        /// this pass. The line is an L from the parent to its last child, with a stub reaching
        /// every child on the way. Each child draws the whole drop from the parent to itself
        /// rather than the piece since the previous sibling, so no row has to know which sibling
        /// came before it or whether a filter hid one; the segments overlap, and the guide colour
        /// is opaque so the overlap does not show.
        /// </summary>
        public void Hang(object key, Rect rect, int depth, object parentKey)
        {
            _rects[key] = rect;

            if (parentKey == null || !_rects.TryGetValue(parentKey, out Rect parentRect)) return;

            float column = Mathf.Round(SlotX(parentRect, depth - 1) + SLOT_WIDTH / 2f);
            float middle = Mathf.Round(rect.y + FlowRowPainter.ROW_HEIGHT / 2f);
            float slot = SlotX(rect, depth);

            _rows.DrawGuide(new Rect(column, parentRect.yMax, 1f, middle - parentRect.yMax));
            _rows.DrawGuide(new Rect(column, middle, slot - GUIDE_GAP - column, 1f));
        }
    }
}

#endif