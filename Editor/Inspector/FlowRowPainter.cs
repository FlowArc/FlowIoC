#if UNITY_EDITOR

using System.Collections.Generic;
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
    public class FlowRowPainter
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

        /// <summary>How much darker than a quiet row a shaded one is, and how much darker its stripe.</summary>
        private const float SHADE = 0.10f;

        private const float STRIPE_SHADE = 0.45f;

        /// <summary>
        /// How much of its own colour a rect keeps under a disabled group - the half Unity leaves a
        /// disabled control - and the window backgrounds it is blended toward, one per skin.
        /// </summary>
        private const float DISABLED_STRENGTH = 0.5f;

        private static readonly Color DarkBackground = new Color(0.22f, 0.22f, 0.22f);
        private static readonly Color LightBackground = new Color(0.76f, 0.76f, 0.76f);

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
        private readonly GUIStyle[] _muted = new GUIStyle[2];
        private readonly GUIStyle[] _miniWrapped = new GUIStyle[2];
        private readonly GUIStyle[] _nameWrapped = new GUIStyle[2];
        private readonly GUIStyle[] _mutedWrapped = new GUIStyle[2];
        private readonly GUIStyle[] _badge = new GUIStyle[2];

        private readonly Dictionary<Color, GUIStyle> _badgeIn = new Dictionary<Color, GUIStyle>();

        private GUIStyle _heading;
        private GUIStyle _icon;
        private GUIStyle _arrow;
        private GUIStyle _action;

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

        /// <summary>
        /// The button under the list - Fix All, Publish All Changed. Green because it is the
        /// window's action, not because of what the rows above it say: a button tinted with the
        /// state it acts on says the state twice and the action not at all. Brighter than the row
        /// green, because GUI.backgroundColor multiplies the skin's button texture and the calmer
        /// value comes back muddy.
        /// </summary>
        public Color Action { get; } = new Color(0.35f, 0.95f, 0.45f);

        /// <summary>One row's worth of vertical space, reaching both edges of the window.</summary>
        public Rect Row(float height = ROW_HEIGHT) => Bleed(EditorGUILayout.GetControlRect(false, height));

        /// <summary>
        /// The same row kept inside the layout's margin, for a list drawn under a panel header
        /// rather than under the window's own bar: the rows line up with the header's edges, the
        /// way every panel's rows line up with the bar over them.
        /// </summary>
        public Rect RowInset(float height = ROW_HEIGHT) => EditorGUILayout.GetControlRect(false, height);

        /// <summary>
        /// A rect that runs the full width of the window rather than sitting inside the margin a
        /// layout group leaves, so a list reads as one column of rows and not as a stack of boxes.
        /// </summary>
        public Rect Bleed(Rect rect) => new Rect(0f, rect.y, rect.width + rect.x, rect.height);

        /// <summary>The row's tint and the stripe down its left edge.</summary>
        public void Paint(Rect rect, Color accent, float alpha = FILL_ALPHA)
        {
            Fill(rect, new Color(accent.r, accent.g, accent.b, alpha));
            Fill(new Rect(rect.x, rect.y, STRIPE_WIDTH, rect.height), accent);
        }

        /// <summary>
        /// Takes a band of an already painted block down, the way the header bar's strip sits
        /// under its title: the column names then read as a layer of the heading rather than as a
        /// row of their own, and no gap between two rects can show through as a line.
        /// </summary>
        public void Darken(Rect rect, float alpha = 0.22f)
        {
            Fill(rect, new Color(0f, 0f, 0f, alpha));
        }

        /// <summary>
        /// Every rect this painter draws goes through here, so a disabled group dims it. Unity
        /// draws a disabled control at half strength inside GUIStyle.Draw, and a rect painted with
        /// EditorGUI.DrawRect is not a control: under a disabled group the row tints, the stripes
        /// and the guide lines stayed at full colour while every word around them dimmed.
        ///
        /// Dimmed by blending toward the window's background rather than by halving the alpha,
        /// which keeps an opaque line opaque. The guide segments overlap by design, and a
        /// translucent one would draw a darker joint at every overlap.
        /// </summary>
        private void Fill(Rect rect, Color color)
        {
            if (!GUI.enabled)
            {
                Color background = EditorGUIUtility.isProSkin ? DarkBackground : LightBackground;

                color = new Color(
                    Mathf.Lerp(background.r, color.r, DISABLED_STRENGTH),
                    Mathf.Lerp(background.g, color.g, DISABLED_STRENGTH),
                    Mathf.Lerp(background.b, color.b, DISABLED_STRENGTH),
                    color.a);
            }

            EditorGUI.DrawRect(rect, color);
        }

        /// <summary>
        /// A row that is there to be read and not to be acted on - the folder a tree hangs from, a
        /// module that cannot host what is being made, a folder that has been left out: the quiet
        /// tint taken down a few tones, the stripe further still so the edge says it too. A few
        /// tones rather than a blackout, so the row keeps its place in the list and its name
        /// stays legible in <see cref="Muted"/>.
        /// </summary>
        public void PaintShaded(Rect rect, Color accent)
        {
            Paint(rect, accent, QUIET_ALPHA);
            Darken(rect, SHADE);
            Darken(new Rect(rect.x, rect.y, STRIPE_WIDTH, rect.height), STRIPE_SHADE);
        }

        /// <summary>
        /// The line a nested row hangs from: down from the arrow of the row it lives in, then
        /// across to its own. Opaque, so the segments the rows draw one after another overlap
        /// without a darker joint where they meet, and grey rather than a status colour - the
        /// line says where a row sits, and the row's tint already says how it is.
        /// </summary>
        public Color Guide { get; } = new Color(0.55f, 0.55f, 0.55f);

        /// <summary>Where a row's first cell starts: past the stripe, with air after it.</summary>
        public float ContentX => STRIPE_WIDTH + 6f;

        /// <summary>One segment of a guide line, a pixel thick in whichever direction it runs.</summary>
        public void DrawGuide(Rect line)
        {
            Fill(line, Guide);
        }

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
        /// A name at its full size but taken down to the quiet grey: a row that is there to be read
        /// and not to be picked - the folder a tree hangs from, a module that cannot host what is
        /// being made. Smaller text would say the row matters less; it does not, it only does less.
        /// </summary>
        public GUIStyle Muted()
        {
            return Style(_muted, false, EditorStyles.label, TextAnchor.MiddleLeft, IdleMuted, HoverMuted);
        }

        /// <summary>
        /// The same quiet grey, wrapped, for a row whose text is a sentence rather than a label -
        /// a scanner finding, say. It is built here rather than by copying <see cref="Mini"/>,
        /// because a GUIStyle copied from a style that was itself copied out of EditorStyles
        /// loses the editor skin behind it and comes back black at the wrong size.
        /// </summary>
        public GUIStyle MiniWrapped(bool hovered)
        {
            GUIStyle style = Style(_miniWrapped, hovered, EditorStyles.miniLabel, TextAnchor.MiddleLeft, IdleMuted,
                HoverMuted);

            // Set every call rather than once: the cache hands back the same instance, so this
            // costs an assignment and saves a second cache to say whether it has been done.
            style.wordWrap = true;

            return style;
        }

        /// <summary>
        /// <see cref="Name"/>, wrapped, for a row whose text may outgrow the window - a rename
        /// preview line naming two long namespaces. Built from EditorStyles for the reason
        /// <see cref="MiniWrapped"/> gives, and anchored at the top so a line that wraps reads
        /// from its first line down rather than floating in the middle of a taller row.
        /// </summary>
        public GUIStyle NameWrapped(bool hovered)
        {
            GUIStyle style = Style(_nameWrapped, hovered, EditorStyles.label, TextAnchor.UpperLeft, IdleText, HoverText);

            style.wordWrap = true;

            return style;
        }

        /// <summary>The quiet grey of <see cref="Muted"/>, wrapped, for the same reason as <see cref="NameWrapped"/>.</summary>
        public GUIStyle MutedWrapped()
        {
            GUIStyle style = Style(_mutedWrapped, false, EditorStyles.label, TextAnchor.UpperLeft, IdleMuted, HoverMuted);

            style.wordWrap = true;

            return style;
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
        /// The same badge in a colour of the caller's - a role's accent, for a row whose kind the
        /// inspector paints. It does not brighten under the pointer: the colour is the point, and
        /// a hover shade of it would read as a different role.
        /// </summary>
        public GUIStyle BadgeIn(Color accent)
        {
            if (_badgeIn.TryGetValue(accent, out GUIStyle style)) return style;

            style = new GUIStyle(EditorStyles.miniBoldLabel) {alignment = TextAnchor.MiddleRight};

            style.normal.textColor = accent;
            style.hover.textColor = accent;
            style.active.textColor = accent;
            style.focused.textColor = accent;

            _badgeIn[accent] = style;

            return style;
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

        /// <summary>
        /// The button under a list: tall enough to read as the panel's action, and inset a little
        /// on every side - the rows above run edge to edge, and a button that did the same would
        /// not read as a button. Every window with one draws it the same, so it is built here
        /// rather than copied into each of them.
        /// </summary>
        public GUIStyle ActionButton => _action ??= new GUIStyle(GUI.skin.button)
        {
            fixedHeight = 36f,
            margin = new RectOffset(6, 6, 4, 6)
        };

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