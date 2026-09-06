#if UNITY_EDITOR

using FlowIoC.BaseModule.Attributes;
using FlowIoC.Editor.Inspector;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.Help
{
    /// <summary>
    /// Every colour and style the help window draws with, resolved once per window against the
    /// current skin. Styles are built lazily because a GUIStyle cannot be constructed before the
    /// first GUI call.
    /// </summary>
    internal class HelpTheme
    {
        private readonly bool _pro = EditorGUIUtility.isProSkin;
        private readonly FlowPalette _palette = new FlowPalette();

        private GUIStyle _heading;
        private GUIStyle _subHeading;
        private GUIStyle _body;
        private GUIStyle _rule;
        private GUIStyle _code;
        private GUIStyle _nodeTitle;
        private GUIStyle _nodeSubtitle;
        private GUIStyle _treeName;
        private GUIStyle _treeComment;
        private GUIStyle _edgeLabel;
        private GUIStyle _caption;
        private GUIStyle _sidebarButton;
        private GUIStyle _sidebarLabel;
        private GUIStyle _bannerTab;
        private GUIStyle _actionButton;
        private GUIStyle _hero;
        private GUIStyle _heroTagline;
        private GUIStyle _partTitle;
        private GUIStyle _partSummary;
        private GUIStyle _partSignature;
        private GUIStyle _codeCaption;

        public Color NodeFill => _pro ? new Color(0.24f, 0.24f, 0.26f) : new Color(0.90f, 0.90f, 0.92f);
        public Color NodeFillActive => _pro ? new Color(0.18f, 0.31f, 0.43f) : new Color(0.76f, 0.87f, 0.98f);
        public Color NodeBorder => _pro ? new Color(0.35f, 0.35f, 0.38f) : new Color(0.68f, 0.68f, 0.72f);
        public Color NodeBorderActive => _pro ? new Color(0.40f, 0.66f, 0.94f) : new Color(0.18f, 0.45f, 0.78f);
        public Color Arrow => _pro ? new Color(0.55f, 0.55f, 0.58f) : new Color(0.45f, 0.45f, 0.50f);
        public Color ArrowActive => _pro ? new Color(0.40f, 0.66f, 0.94f) : new Color(0.18f, 0.45f, 0.78f);
        public Color ArrowForbidden => _pro ? new Color(0.85f, 0.36f, 0.33f) : new Color(0.75f, 0.22f, 0.20f);
        public Color CodeFill => _pro ? new Color(0.16f, 0.16f, 0.17f) : new Color(0.94f, 0.94f, 0.95f);

        /// <summary>
        /// What the page itself is drawn on. An arrow's marking carries a strip of it, so a word
        /// wider than the gap between two boxes still reads as one word rather than as two halves
        /// in two different shades.
        /// </summary>
        public Color PageFill => _pro ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.76f, 0.76f, 0.76f);

        public Color MutedText => _pro ? new Color(0.66f, 0.66f, 0.69f) : new Color(0.40f, 0.40f, 0.44f);

        /// <summary>
        /// The banner behind a page title. Root's colour from the inspector palette, so the help
        /// window and the bar on top of a Root read as one tool - and dark enough that the white
        /// title on it clears 4.5:1, which the lighter purple it used to be did not.
        /// </summary>
        public Color Banner => _palette.ChromeDeep;

        public float BannerHeight => 38f;

        public float BannerTabHeight => 26f;

        public float BannerTabWidth => 104f;

        /// <summary>
        /// The green a page's own action wears. Dark enough that white text sits on it, and far
        /// enough from the banner's purple that the button does not read as part of the bar.
        /// </summary>
        public Color Action => new Color(.29f, .74f, .38f);

        public float ActionWidth => 108f;

        public float ActionHeight => 30f;

        /// <summary>
        /// A reading of the page, as a button on the banner. Larger than a toolbar button, because
        /// this is the one control on the page a reader has to notice.
        /// </summary>
        public GUIStyle BannerTab => _bannerTab ??= new GUIStyle(GUI.skin.button)
        {
            fixedHeight = 0f,
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            padding = new RectOffset(10, 10, 4, 4),
            margin = new RectOffset(0, 0, 0, 0)
        };

        /// <summary>
        /// A page's own action. Bigger type than the tabs beside it, and white in both skins:
        /// the button carries its own colour, so the Editor's text colour has nothing to do with
        /// what it is sitting on.
        /// </summary>
        public GUIStyle ActionButton => _actionButton ??= new GUIStyle(GUI.skin.button)
        {
            fixedHeight = 0f,
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            padding = new RectOffset(10, 10, 4, 4),
            margin = new RectOffset(0, 0, 0, 0),
            normal = {textColor = Color.white},
            hover = {textColor = Color.white},
            active = {textColor = Color.white},
            focused = {textColor = Color.white}
        };

        /// <summary>
        /// The page title, drawn on the banner. It is built from whiteLabel rather than
        /// boldLabel because the purple behind it is the same in both skins.
        /// </summary>
        public GUIStyle Heading => _heading ??= new GUIStyle(EditorStyles.whiteLabel)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            richText = true
        };

        public GUIStyle SubHeading => _subHeading ??= new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 12,
            margin = new RectOffset(0, 0, 8, 4)
        };

        public GUIStyle Body => _body ??= new GUIStyle(EditorStyles.label)
        {
            wordWrap = true,
            margin = new RectOffset(0, 0, 2, 6)
        };

        public GUIStyle Rule => _rule ??= new GUIStyle(EditorStyles.label)
        {
            wordWrap = true,
            fontStyle = FontStyle.Italic,
            margin = new RectOffset(0, 0, 6, 4)
        };

        public GUIStyle Code => _code ??= new GUIStyle(EditorStyles.textArea)
        {
            font = EditorStyles.miniFont,
            fontSize = 11,
            wordWrap = false,
            padding = new RectOffset(8, 8, 6, 6),
            margin = new RectOffset(0, 0, 2, 8)
        };

        public GUIStyle NodeTitle => _nodeTitle ??= new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.LowerCenter,
            fontSize = 11,
            wordWrap = true
        };

        public GUIStyle NodeSubtitle => _nodeSubtitle ??= new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.UpperCenter,
            wordWrap = true
        };

        public GUIStyle TreeName => _treeName ??= new GUIStyle(EditorStyles.label)
        {
            font = EditorStyles.miniFont,
            fontSize = 11
        };

        public GUIStyle TreeComment => _treeComment ??= new GUIStyle(EditorStyles.miniLabel)
        {
            font = EditorStyles.miniFont,
            fontSize = 11
        };

        public GUIStyle EdgeLabel => _edgeLabel ??= new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter
        };

        /// <summary>
        /// The caption under a screenshot. Muted and centred, so it reads as a label on the
        /// picture rather than as another paragraph of the page.
        /// </summary>
        public GUIStyle Caption => _caption ??= new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            fontStyle = FontStyle.Italic,
            margin = new RectOffset(0, 0, 2, 8)
        };

        /// <summary>The hairline drawn around a screenshot, so it reads as a picture and not as page.</summary>
        public Color ImageBorder => _pro ? new Color(0.35f, 0.35f, 0.38f) : new Color(0.68f, 0.68f, 0.72f);

        /// <summary>
        /// What the page around a mark leaves to it: the sidebar the window draws beside the
        /// content, plus room for the scroll bar and the margins on either side. Anything that
        /// has to know how wide the page is before the layout pass measures it - a screenshot, a
        /// row of cards - subtracts this from the view width.
        /// </summary>
        public float ContentMargin => 300f;

        /// <summary>What a screenshot leaves to the page around it.</summary>
        public float ImageMargin => ContentMargin;

        /// <summary>
        /// The one line a page opens with. Large enough to be read before the paragraph under it
        /// and before the diagram below that, because it is the sentence a reader leaves with.
        /// </summary>
        public GUIStyle Hero => _hero ??= new GUIStyle(EditorStyles.label)
        {
            fontSize = 17,
            fontStyle = FontStyle.Bold,
            wordWrap = true,
            margin = new RectOffset(0, 0, 8, 2)
        };

        /// <summary>
        /// The line under the headline. Larger than body text and drawn muted, so the two read as
        /// one opening rather than as a heading followed by a paragraph.
        /// </summary>
        public GUIStyle HeroTagline => _heroTagline ??= new GUIStyle(EditorStyles.label)
        {
            fontSize = 12,
            wordWrap = true,
            margin = new RectOffset(0, 0, 0, 10)
        };

        /// <summary>The name of a part, on its card.</summary>
        public GUIStyle PartTitle => _partTitle ??= new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleLeft,
            wordWrap = false,
            padding = new RectOffset(0, 0, 0, 0),
            margin = new RectOffset(0, 0, 0, 0)
        };

        /// <summary>What the part is for, in the one or two lines under its name.</summary>
        public GUIStyle PartSummary => _partSummary ??= new GUIStyle(EditorStyles.label)
        {
            fontSize = 11,
            wordWrap = true,
            padding = new RectOffset(0, 0, 0, 0),
            margin = new RectOffset(0, 0, 0, 0)
        };

        /// <summary>
        /// What the part looks like in code, at the foot of its card. The code font, so a reader
        /// scanning the two cards sees a signature rather than another sentence.
        /// </summary>
        public GUIStyle PartSignature => _partSignature ??= new GUIStyle(EditorStyles.miniLabel)
        {
            font = EditorStyles.miniFont,
            fontSize = 11,
            wordWrap = false,
            padding = new RectOffset(0, 0, 0, 0),
            margin = new RectOffset(0, 0, 0, 0)
        };

        /// <summary>
        /// Which file a code block is from, drawn muted above it. A snippet that says where it
        /// belongs is worth more than the same snippet with the path commented into its first line.
        /// </summary>
        public GUIStyle CodeCaption => _codeCaption ??= new GUIStyle(EditorStyles.miniLabel)
        {
            font = EditorStyles.miniFont,
            fontSize = 11,
            margin = new RectOffset(0, 0, 6, 0)
        };

        /// <summary>The fill and the hairline of a part card, shared with the boxes in a diagram.</summary>
        public Color CardFill => NodeFill;

        public Color CardBorder => NodeBorder;

        /// <summary>The gap between two cards, and what a card keeps clear inside its own edges.</summary>
        public float CardGap => 10f;

        public float CardPadding => 12f;

        /// <summary>
        /// The narrowest a card may be drawn. Below this the summary wraps to a column of single
        /// words, so the row runs past the page instead - the window scrolls, the card stays read.
        /// </summary>
        public float CardMinWidth => 190f;

        /// <summary>One box for every card icon, whatever the icon's own resolution.</summary>
        public float CardIconSize => 20f;

        /// <summary>
        /// The text on a topic row. The window draws it into a rectangle of its own, so this
        /// style carries no padding: the alignment is the window's to decide.
        /// </summary>
        public GUIStyle SidebarLabel => _sidebarLabel ??= new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 12,
            wordWrap = true,
            padding = new RectOffset(0, 0, 0, 0),
            margin = new RectOffset(0, 0, 0, 0)
        };

        public GUIStyle SidebarButton => _sidebarButton ??= new GUIStyle(EditorStyles.miniButton)
        {
            fixedHeight = 0f,
            fontSize = 12,
            wordWrap = true,
            alignment = TextAnchor.MiddleLeft,
            imagePosition = ImagePosition.ImageLeft,
            padding = new RectOffset(8, 8, 6, 6),
            margin = new RectOffset(2, 2, 2, 2)
        };
    }
}

#endif