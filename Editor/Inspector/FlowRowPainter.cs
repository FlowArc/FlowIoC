#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.Inspector
{
    /// <summary>
    /// The list a FlowIoC window draws under its header bar: a row per thing, running the full
    /// width the bar does, tinted by what the row has to say about itself.
    ///
    /// The colours are about a row's state rather than about a role, which is why they are here
    /// and not in FlowPalette. A settled row is green in every window that has one - Module Scanner
    /// means "nothing is wrong with this module", the Screen Scanner means "no other screen opens
    /// on this layer" - and a reader who learns the colour once has learned it everywhere.
    /// </summary>
    internal class FlowRowPainter
    {
        public const float ROW_HEIGHT = 20f;
        public const float STRIPE_WIDTH = 3f;

        /// <summary>What a settled row is filled with.</summary>
        public const float FILL_ALPHA = 0.16f;

        /// <summary>A heading over a group of rows, filled harder so the group reads as one.</summary>
        public const float HEADING_ALPHA = 0.28f;

        /// <summary>
        /// A row in a list long enough that filling every one of them the usual amount would read
        /// as a wall of colour - forty modules that are all in order, say.
        /// </summary>
        public const float QUIET_ALPHA = 0.08f;

        /// <summary>
        /// What a row's text sits at while the pointer is elsewhere. Off white rather than white,
        /// so a row has somewhere to go when the pointer arrives.
        /// </summary>
        private static readonly Color IdleText = new Color(0.82f, 0.82f, 0.82f);

        private static readonly Color HoverText = Color.white;

        /// <summary>What a row says quietly, and the same again once the row is under the pointer.</summary>
        private static readonly Color IdleMuted = new Color(0.62f, 0.62f, 0.62f);

        private static readonly Color HoverMuted = new Color(0.88f, 0.88f, 0.88f);

        private readonly GUIStyle[] _name = new GUIStyle[2];
        private readonly GUIStyle[] _strong = new GUIStyle[2];
        private readonly GUIStyle[] _cell = new GUIStyle[2];
        private readonly GUIStyle[] _mini = new GUIStyle[2];
        private readonly GUIStyle[] _badge = new GUIStyle[2];

        private GUIStyle _heading;
        private GUIStyle _icon;
        private GUIStyle _arrow;

        /// <summary>Nothing to report. Green because it is settled, not because it was checked.</summary>
        public Color Ok { get; } = new Color(0.42f, 0.78f, 0.47f);

        /// <summary>
        /// Something worth looking at that the editor still allows - a repair waiting to run, two
        /// screens on one layer. Amber, never red: this is a warning and not a refusal.
        /// </summary>
        public Color Warn { get; } = new Color(1f, 0.8f, 0.35f);

        /// <summary>Something only a person can settle.</summary>
        public Color Error { get; } = new Color(0.94f, 0.44f, 0.4f);

        /// <summary>
        /// The header bar over a list like this: the row green taken down until white title text
        /// clears 4.5:1, so the bar and the rows under it read as one colour.
        /// </summary>
        public Color Bar { get; } = new Color(0.165f, 0.431f, 0.22f);

        /// <summary>One row's worth of vertical space, reaching both edges of the window.</summary>
        public Rect Row(float height = ROW_HEIGHT) => Bleed(EditorGUILayout.GetControlRect(false, height));

        /// <summary>
        /// A rect that runs the full width of the window rather than sitting inside the margin a
        /// layout group leaves, so a list reads as one column of rows and not as a stack of boxes.
        /// </summary>
        public Rect Bleed(Rect rect) => new Rect(0f, rect.y, rect.width + rect.x, rect.height);

        /// <summary>The row's tint and the stripe down its left edge.</summary>
        public void Paint(Rect rect, Color accent, float alpha = FILL_ALPHA)
        {
            EditorGUI.DrawRect(rect, new Color(accent.r, accent.g, accent.b, alpha));
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, STRIPE_WIDTH, rect.height), accent);
        }

        /// <summary>
        /// Takes a band of an already painted block down, the way the header bar's strip sits
        /// under its title: the column names then read as a layer of the heading rather than as a
        /// row of their own, and no gap between two rects can show through as a line.
        /// </summary>
        public void Darken(Rect rect, float alpha = 0.22f)
        {
            EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, alpha));
        }

        /// <summary>Where a row's first cell starts: past the stripe, with air after it.</summary>
        public float ContentX => STRIPE_WIDTH + 6f;

        /// <summary>
        /// Whether the pointer is over this row. A row lights up as a whole rather than one cell
        /// at a time, so the reader is told which line they are on and not which word.
        /// </summary>
        public bool IsHovered(Rect rect)
        {
            return Event.current != null && rect.Contains(Event.current.mousePosition);
        }

        // EditorStyles is not loaded when a window's fields are, so every style is built on use.

        /// <summary>
        /// What the row is called. Plain rather than bold: a list where every row is bold has no
        /// emphasis left for the heading over it, and the tint behind the row carries the colour.
        /// </summary>
        public GUIStyle Name(bool hovered)
        {
            return Style(_name, hovered, EditorStyles.label, TextAnchor.MiddleLeft, IdleText, HoverText);
        }

        /// <summary>The one line that is a heading rather than a row, and is bold for it.</summary>
        public GUIStyle Strong(bool hovered)
        {
            return Style(_strong, hovered, EditorStyles.boldLabel, TextAnchor.MiddleLeft, IdleText, HoverText);
        }

        /// <summary>A cell beside the name, in the same grey.</summary>
        public GUIStyle Cell(bool hovered)
        {
            return Style(_cell, hovered, EditorStyles.label, TextAnchor.MiddleLeft, IdleText, HoverText);
        }

        /// <summary>What a row says quietly - an assembly name, a column heading.</summary>
        public GUIStyle Mini(bool hovered)
        {
            return Style(_mini, hovered, EditorStyles.miniLabel, TextAnchor.MiddleLeft, IdleMuted, HoverMuted);
        }

        /// <summary>
        /// A column name over a list, in the colour of the list under it - the way the header
        /// bar's strip names the module in the role's own accent rather than in white.
        /// </summary>
        public GUIStyle Heading(Color color)
        {
            _heading ??= new GUIStyle(EditorStyles.miniLabel) {alignment = TextAnchor.MiddleLeft};

            // Every state, so a column name never brightens under the pointer. Nothing happens
            // when it is clicked, and a heading that lights up says otherwise.
            _heading.normal.textColor = color;
            _heading.hover.textColor = color;
            _heading.active.textColor = color;
            _heading.focused.textColor = color;

            return _heading;
        }

        /// <summary>What kind of thing the row is, against its right edge.</summary>
        public GUIStyle Badge(bool hovered)
        {
            return Style(_badge, hovered, EditorStyles.miniLabel, TextAnchor.MiddleRight, IdleMuted, HoverMuted);
        }

        /// <summary>
        /// One of a style's two states, built once. Every state of the style carries the same
        /// colour: a label drawn as a button would otherwise take GUI's own hover colour for the
        /// cell under the pointer, which is the opposite of lighting the whole row.
        /// </summary>
        private GUIStyle Style(GUIStyle[] cache, bool hovered, GUIStyle from, TextAnchor alignment, Color idle,
            Color hover)
        {
            int index = hovered ? 1 : 0;

            if (cache[index] != null) return cache[index];

            Color color = hovered ? hover : idle;
            var style = new GUIStyle(from) {alignment = alignment};

            style.normal.textColor = color;
            style.hover.textColor = color;
            style.active.textColor = color;
            style.focused.textColor = color;
            style.onNormal.textColor = color;
            style.onHover.textColor = color;
            style.onActive.textColor = color;
            style.onFocused.textColor = color;

            cache[index] = style;

            return style;
        }

        public GUIStyle Icon => _icon ??= new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 11
        };

        /// <summary>
        /// The triangle that says whether a row is open. At the label's own size, drawn with the
        /// light glyphs: a smaller one read as decoration rather than as the control it is, and
        /// the solid triangles at this size sit heavier than the name beside them.
        /// </summary>
        public GUIStyle Arrow => _arrow ??= new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 14
        };
    }
}

#endif