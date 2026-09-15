#if UNITY_EDITOR

using System;
using FlowIoC.Editor.Inspector;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.ModulePanels
{
    /// <summary>
    /// The marks the list down a panel's left side is drawn with: a heading for a group of things,
    /// an item that is picked by clicking it, and a button under the group - the green one that
    /// makes a new item, usually. One item is the selected one and is drawn in the panel's colour;
    /// an item may wear a small word at its right edge, "active", for what is true of it beyond
    /// being listed. The panel says which item is selected and what a click does; the sidebar
    /// keeps nothing.
    ///
    /// An item lights up under the pointer because clicking it does something. A heading does
    /// not, for the reason the panel's own rows do not.
    /// </summary>
    public class ModulePanelSidebarPainter
    {
        private const float ITEM_HEIGHT = 22f;
        private const float ACTION_HEIGHT = 26f;
        private const float BUTTON_HEIGHT = 18f;
        private const float ADD_HEIGHT = 22f;
        private const float RIGHT_MARGIN = 8f;
        private const float MARKER_GAP = 6f;
        private const float SQUARE = 18f;
        private const float HEADING_HEIGHT = FlowRowPainter.ROW_HEIGHT * 2f;

        private readonly FlowRowPainter _rows;
        private readonly FlowPalette _palette;
        private readonly Color _accent;

        private GUIStyle _square;

        internal ModulePanelSidebarPainter(FlowRowPainter rows, FlowPalette palette, Color accent)
        {
            _rows = rows;
            _palette = palette;
            _accent = accent;
        }

        /// <summary>
        /// A group's name over its items, drawn the way the bar's strip names the module: a band
        /// taken down a step, the words in the panel's colour and in capitals. It is nothing like
        /// an item, selected or not, so the list is read as a title over rows rather than as one
        /// more row that happens to be lit.
        /// </summary>
        public void Heading(string text)
        {
            Rect rect = _rows.Row(HEADING_HEIGHT);
            PaintHeading(rect);

            GUI.Label(Content(rect), text.ToUpperInvariant(), _rows.Heading(_accent));
        }

        /// <summary>
        /// A group's name with one small square button at the right edge of the same row - the "+"
        /// that adds an item to the group, green like every button that adds. The label is the
        /// button's whole face, so it is a glyph, not a word.
        /// </summary>
        public void Heading(string text, ModulePanelAction action)
        {
            Rect rect = _rows.Row(HEADING_HEIGHT);
            PaintHeading(rect);

            Rect content = Content(rect);
            var button = new Rect(content.xMax - SQUARE, rect.y + (rect.height - SQUARE) / 2f, SQUARE, SQUARE);

            Color previous = GUI.backgroundColor;

            if (action.Kind == ModulePanelActionKind.Add)
                GUI.backgroundColor = _palette.ActionAdd;
            else if (action.Kind == ModulePanelActionKind.Remove)
                GUI.backgroundColor = _palette.ActionRemove;

            using (new EditorGUI.DisabledScope(!action.Enabled || action.OnClick == null))
            {
                if (GUI.Button(button, action.Label, SquareStyle()))
                    action.OnClick();
            }

            GUI.backgroundColor = previous;

            content.width = Mathf.Max(0f, button.x - MARKER_GAP - content.x);
            GUI.Label(content, text.ToUpperInvariant(), _rows.Heading(_accent));
        }

        /// <summary>
        /// One thing in the list. The selected one wears the panel's colour; the others light up
        /// under the pointer and go to <paramref name="onClick"/> when pressed. The marker is a
        /// word at the right edge, or null for none.
        /// </summary>
        public void Item(string label, bool selected, string marker, Action onClick)
        {
            Rect rect = _rows.Row(ITEM_HEIGHT);
            bool hovered = !selected && _rows.IsHovered(rect);

            _rows.Paint(rect, _accent,
                selected ? FlowRowPainter.HEADING_ALPHA : hovered ? FlowRowPainter.FILL_ALPHA : FlowRowPainter.QUIET_ALPHA);

            if (GUI.Button(rect, GUIContent.none, GUIStyle.none) && onClick != null)
                onClick();

            Rect content = Content(rect);
            float markerWidth = 0f;

            if (!string.IsNullOrEmpty(marker))
            {
                GUIStyle mini = _rows.Mini(hovered);
                markerWidth = mini.CalcSize(new GUIContent(marker)).x;
                GUI.Label(new Rect(content.xMax - markerWidth, content.y, markerWidth, content.height), marker, mini);
                markerWidth += MARKER_GAP;
            }

            GUI.Label(new Rect(content.x, content.y, content.width - markerWidth, content.height), label,
                selected ? _rows.Strong(false) : _rows.Name(hovered));
        }

        /// <summary>
        /// A button under a group - the one that adds an item is the whole width of the list,
        /// green, the way the panel's own rows draw a button that adds; any other is a mini button
        /// at the left.
        /// </summary>
        public void Action(ModulePanelAction action)
        {
            Rect rect = _rows.Row(ACTION_HEIGHT);
            _rows.Paint(rect, _accent, FlowRowPainter.QUIET_ALPHA);

            Rect content = Content(rect);
            bool adds = action.Kind == ModulePanelActionKind.Add;
            float height = adds ? ADD_HEIGHT : BUTTON_HEIGHT;
            float width = adds
                ? content.width
                : EditorStyles.miniButton.CalcSize(new GUIContent(action.Label)).x + 16f;
            var button = new Rect(content.x, rect.y + (rect.height - height) / 2f, width, height);

            Color previous = GUI.backgroundColor;

            if (adds)
                GUI.backgroundColor = _palette.ActionAdd;
            else if (action.Kind == ModulePanelActionKind.Remove)
                GUI.backgroundColor = _palette.ActionRemove;

            using (new EditorGUI.DisabledScope(!action.Enabled || action.OnClick == null))
            {
                if (GUI.Button(button, action.Label, adds ? GUI.skin.button : EditorStyles.miniButton))
                    action.OnClick();
            }

            GUI.backgroundColor = previous;
        }

        public void Space() => GUILayout.Space(6f);

        /// <summary>The quiet tint of a row taken down a step, the way the bar's strip sits under its title.</summary>
        private void PaintHeading(Rect rect)
        {
            _rows.Paint(rect, _accent, FlowRowPainter.QUIET_ALPHA);
            _rows.Darken(rect);
        }

        /// <summary>
        /// A mini button that is exactly the rect it is given: the skin's mini button carries a fixed
        /// height and an overflow that would stretch a square into a low rectangle, so both are
        /// cleared, and the padding with them, so a single glyph sits centred.
        /// </summary>
        private GUIStyle SquareStyle()
        {
            return _square ??= new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, 0, 0),
                overflow = new RectOffset(0, 0, 0, 0),
                fixedHeight = 0f,
                fixedWidth = 0f,
                fontStyle = FontStyle.Bold
            };
        }

        private Rect Content(Rect row) =>
            new Rect(row.x + _rows.ContentX, row.y, row.width - _rows.ContentX - RIGHT_MARGIN, row.height);
    }
}

#endif
