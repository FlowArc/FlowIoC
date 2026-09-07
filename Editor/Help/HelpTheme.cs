#if UNITY_EDITOR

using System.Collections.Generic;
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
        private GUIStyle _sidebarRow;
        private GUIStyle _sidebarLabel;
        private GUIStyle _sidebarLabelActive;
        private GUIStyle _header;
        private GUIStyle _tabLabel;
        private GUIStyle _tabLabelActive;
        private readonly List<Texture2D> _fills = new List<Texture2D>();
        private GUIStyle _bannerTab;
        private GUIStyle _actionButton;
        private GUIStyle _hero;
        private GUIStyle _heroTagline;
        private GUIStyle _partTitle;
        private GUIStyle _partSummary;
        private GUIStyle _partSignature;
        private GUIStyle _codeCaption;
        private GUIStyle _codeCopy;

        public Color NodeFill => _pro ? new Color(0.24f, 0.24f, 0.26f) : new Color(0.90f, 0.90f, 0.92f);
        public Color NodeFillActive => _pro ? new Color(0.18f, 0.31f, 0.43f) : new Color(0.76f, 0.87f, 0.98f);
        public Color NodeBorder => _pro ? new Color(0.35f, 0.35f, 0.38f) : new Color(0.68f, 0.68f, 0.72f);
        public Color NodeBorderActive => _pro ? new Color(0.40f, 0.66f, 0.94f) : new Color(0.18f, 0.45f, 0.78f);
        public Color Arrow => _pro ? new Color(0.55f, 0.55f, 0.58f) : new Color(0.45f, 0.45f, 0.50f);
        public Color ArrowActive => _pro ? new Color(0.40f, 0.66f, 0.94f) : new Color(0.18f, 0.45f, 0.78f);
        public Color ArrowForbidden => _pro ? new Color(0.85f, 0.36f, 0.33f) : new Color(0.75f, 0.22f, 0.20f);

        /// <summary>What a snippet is drawn on: Rider's own editor background, in both skins.</summary>
        public Color CodeFill => _pro ? Hex(0x262626) : Hex(0xFFFFFF);

        /// <summary>The hairline around a code block, so it reads as a listing and not as page.</summary>
        public Color CodeBorder => NodeBorder;

        /// <summary>
        /// What a snippet is coloured with. The values are lifted from Rider's own schemes - Rider
        /// Dark under the dark skin and Rider Light under the light one, the two the theme pack
        /// ships and the pair a Rider window is almost always showing - so a snippet in the help
        /// window and the same lines open in the IDE are the same picture.
        ///
        /// Every kind Rider gives a colour of its own is here, type names and fields included.
        /// Leaving those in the body colour is what Darcula does, and this is not Darcula.
        /// </summary>
        public Color CodeText => _pro ? Hex(0xBDBDBD) : Hex(0x383838);

        public Color CodeKeyword => _pro ? Hex(0x6C95EB) : Hex(0x0F54D6);

        public Color CodeString => _pro ? Hex(0xC9A26D) : Hex(0x8C6C41);

        public Color CodeNumber => _pro ? Hex(0xED94C0) : Hex(0xAB2F6B);

        public Color CodeComment => _pro ? Hex(0x85C46C) : Hex(0x248700);

        public Color CodeMethod => _pro ? Hex(0x39CC9B) : Hex(0x00855F);

        /// <summary>A class, an interface, an attribute or a namespace - Rider draws all four alike.</summary>
        public Color CodeType => _pro ? Hex(0xC191FF) : Hex(0x6B2FBA);

        /// <summary>A field or a property, which is what the name after a dot almost always is.</summary>
        public Color CodeField => _pro ? Hex(0x66C3CC) : Hex(0x0093A1);

        /// <summary>What a copy button keeps to itself, at the right of a code block's caption row.</summary>
        public float CodeCopyWidth => 52f;

        private static Color Hex(int value) => new Color(
            ((value >> 16) & 0xFF) / 255f,
            ((value >> 8) & 0xFF) / 255f,
            (value & 0xFF) / 255f);

        /// <summary>
        /// What the page is drawn on. Darker than the Editor's own window grey, which is what puts
        /// the three surfaces of this window a clear step apart: the menu at the top of the range,
        /// the band that says what the page is about a shade under it, and the page itself at the
        /// bottom. An arrow's marking carries a strip of this, so a word wider than the gap between
        /// two boxes still reads as one word rather than as two halves in two different shades.
        /// </summary>
        public Color PageFill => _pro ? Hex(0x313131) : Hex(0xBBBBBB);

        public Color MutedText => _pro ? new Color(0.66f, 0.66f, 0.69f) : new Color(0.40f, 0.40f, 0.44f);

        /// <summary>
        /// What the sidebar is drawn on. Brighter than the page beside it in both skins, so the
        /// menu reads as a panel of its own rather than as the left edge of the page - which is
        /// what a help box, drawn in the page's own grey, left it looking like.
        ///
        /// Fourteen levels of grey above the page beside it, which is the distance Odin's own menu
        /// keeps from the panel next to it: enough to be seen as a second surface and little enough
        /// that the menu is still the quieter half of the window.
        /// </summary>
        public Color SidebarFill => _pro ? Hex(0x3F3F3F) : Hex(0xC9C9C9);

        /// <summary>
        /// The line that closes the panel off from the page. Darker than anything inside the menu,
        /// so the sidebar has an edge rather than fading into the page at its right.
        /// </summary>
        public Color SidebarBorder => _pro ? Hex(0x232323) : Hex(0xADADAD);

        /// <summary>
        /// The band under the banner, where the page's headline and the paragraph beneath it sit.
        /// A shade under the sidebar and a shade over the page, so the window reads as three
        /// surfaces stepping down: the menu, what the page is about, and the page itself.
        /// </summary>
        public Color HeaderFill => _pro ? Hex(0x3C3C3C) : Hex(0xC6C6C6);

        /// <summary>
        /// The hairline that closes the header off. One pixel and darker than either surface it
        /// sits between, so the band ends cleanly without a bar's worth of weight - the heavy bar
        /// belongs to the page below, where it parts one topic from the next.
        /// </summary>
        public Color HeaderEdge => _pro ? Hex(0x232323) : Hex(0xA8A8A8);

        /// <summary>
        /// What the band under the banner keeps clear at its left and its right. The headline is
        /// the one line on the page set in a large face, so it sits closer to the edge than the
        /// paragraphs below it and still reads as the leftmost thing in the window.
        /// </summary>
        public float PagePadding => 16f;

        /// <summary>
        /// What the page below the band keeps clear on the same two sides. Wider than the header's,
        /// which is the proportion Odin's own panel keeps - a column of body text wants more air
        /// around it than a headline does, and the step in from the band is what says the reading
        /// has started. A mark that has to reach past the text takes this back off again, so the
        /// theme rather than the window or the painter owns the number.
        /// </summary>
        public float PageBodyPadding => 28f;

        /// <summary>
        /// The three colours of the bar a page parts its topics with. It is five pixels rather
        /// than one because it ends a block rather than parting two rows: a dark line, a body of
        /// the same dark, and a light line under it that reads as the page starting again.
        /// </summary>
        public Color PageSeparatorEdge => _pro ? Hex(0x272727) : Hex(0xB1B1B1);

        public Color PageSeparatorFill => _pro ? Hex(0x2B2B2B) : Hex(0xB5B5B5);

        public Color PageSeparatorLight => _pro ? Hex(0x404040) : Hex(0xCACACA);

        /// <summary>
        /// The darker half of the groove under a row, and the lighter half below it. Two hairlines
        /// rather than one: a single line reads as a scratch on the panel, while a dark line with a
        /// light one under it reads as the surface stepping down and back up, which is what tells
        /// one entry from the next now that the rows carry no frame and no gap between them.
        /// </summary>
        public Color SidebarSeparator => _pro ? Hex(0x313131) : Hex(0xBBBBBB);

        public Color SidebarSeparatorLight => _pro ? Hex(0x494949) : Hex(0xD3D3D3);

        /// <summary>
        /// What a row is filled with at the depth it sits. The panel's own colour at the top level,
        /// and a shade darker for every category above it, so a fold that opens reads as a step
        /// down into the panel rather than as more rows of the same surface. It is deliberately
        /// slight: two levels apart should be a difference you feel rather than one you look at.
        /// </summary>
        public Color SidebarRowFill(int depth) =>
            depth <= 0 ? SidebarFill : Color.Lerp(SidebarFill, Color.black, depth * DepthShade);

        /// <summary>How much of a row's fill one level of depth takes away.</summary>
        private float DepthShade => 0.12f;

        /// <summary>
        /// What a row lights up with under the pointer. Every row in the sidebar either goes
        /// somewhere or folds something open, so the highlight only ever promises what a click
        /// will actually do.
        /// </summary>
        public Color SidebarRowHover => _pro
            ? new Color(1f, 1f, 1f, 0.06f)
            : new Color(0f, 0f, 0f, 0.06f);

        /// <summary>
        /// The row the reader is on, filled edge to edge. FlowIoC's own violet, which is what the
        /// banner on the page beside it wears, so the two say together where the reader has landed.
        /// </summary>
        public Color SidebarRowSelected => _palette.ChromeDeep;

        /// <summary>
        /// The top and the bottom line of the selected row, a shade either side of its fill. The
        /// row is lit from above the way every other raised thing in the Editor is, which is what
        /// keeps a flat block of colour from reading as a hole cut in the panel.
        /// </summary>
        public Color SidebarRowSelectedTop => Color.Lerp(SidebarRowSelected, Color.white, 0.10f);

        public Color SidebarRowSelectedBottom => Color.Lerp(SidebarRowSelected, Color.black, 0.28f);

        /// <summary>
        /// A featured topic that is not the one selected. The same violet thinned to a tint, so
        /// the introduction is marked out without being read as where the reader already is.
        /// </summary>
        public Color SidebarRowFeatured => Tint(_palette.ChromeDeep, 0.12f);

        /// <summary>
        /// A colour laid over the panel thinly enough to read as a tint of it. The alpha is what
        /// separates a row that is marked out from the row the reader is actually on: the selected
        /// row carries the violet whole, and a featured one carries this much of it.
        /// </summary>
        private static Color Tint(Color color, float strength) =>
            new Color(color.r, color.g, color.b, strength);

        /// <summary>
        /// The triangle that says whether a category is open. Lighter than the name beside it in
        /// the dark skin and darker in the light one, because it is a control rather than a word:
        /// it should be found when it is looked for and read past when it is not.
        /// </summary>
        public Color SidebarArrow => _pro ? Hex(0xC4C4C4) : Hex(0x3C3C3C);

        /// <summary>The same triangle on the selected row, where everything is drawn white.</summary>
        public Color SidebarArrowActive => _palette.Title;

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

        /// <summary>
        /// A snippet. It is built from label rather than from textArea because the block paints
        /// its own fill and hairline: a text field's background under that would draw the listing
        /// twice. Rich text is what carries the colouring, and the style's own colour is what a
        /// word the highlighter leaves alone - punctuation, a type name - is drawn in.
        /// </summary>
        public GUIStyle Code => _code ??= new GUIStyle(EditorStyles.label)
        {
            font = EditorStyles.miniFont,

            // The size the page's own paragraphs are, rather than the smaller size a snippet is
            // usually shrunk to. A snippet is read a character at a time - a leading underscore, a
            // closing angle bracket - and the help window is the one place in the Editor where the
            // code is the thing being studied.
            fontSize = 12,
            wordWrap = false,
            richText = true,
            padding = new RectOffset(8, 8, 6, 6),
            margin = new RectOffset(0, 0, 2, 8),
            normal = {textColor = CodeText},
            hover = {textColor = CodeText},
            focused = {textColor = CodeText},
            active = {textColor = CodeText}
        };

        /// <summary>
        /// The button that puts a snippet on the clipboard, at the right of the caption row above
        /// it. A code block is drawn rather than typed into, so this is what has replaced dragging
        /// a selection across it - and it copies the snippet as it was written, without the colour
        /// tags the block is drawn from.
        /// </summary>
        public GUIStyle CodeCopy => _codeCopy ??= new GUIStyle(EditorStyles.miniButton)
        {
            fontSize = 10,
            padding = new RectOffset(6, 6, 2, 2),
            margin = new RectOffset(0, 0, 4, 0)
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

        /// <summary>
        /// The text on the row the reader is on. White in both skins, because the fill under it is
        /// the same violet whichever skin the Editor is in.
        /// </summary>
        public GUIStyle SidebarLabelActive => _sidebarLabelActive ??= new GUIStyle(SidebarLabel)
        {
            normal = {textColor = _palette.Title},
            hover = {textColor = _palette.Title},
            focused = {textColor = _palette.Title},
            active = {textColor = _palette.Title}
        };

        /// <summary>
        /// The name on a tab. Centred, because the strip splits the page evenly and a label pushed
        /// to one end of its share would read as belonging to the tab beside it.
        /// </summary>
        public GUIStyle TabLabel => _tabLabel ??= new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 11,
            padding = new RectOffset(0, 0, 0, 0),
            margin = new RectOffset(0, 0, 0, 0)
        };

        /// <summary>The name on the tab that is open, set bold so the fill is not the only mark.</summary>
        public GUIStyle TabLabelActive => _tabLabelActive ??= new GUIStyle(TabLabel)
        {
            fontStyle = FontStyle.Bold
        };

        /// <summary>
        /// A sidebar row. It carries no background and no border of its own: the window fills the
        /// row, draws the hairline under it and places the icon and the text by hand, so the style
        /// is only what makes the rectangle clickable. The margins are zero so two rows touch and
        /// what separates them is one hairline rather than two edges with a gap between them.
        /// </summary>
        /// <summary>
        /// The header band as a style. A group in IMGUI paints a background only when its style
        /// carries one, and the fill has to be under the headline rather than over it, so the band
        /// is a one pixel texture the style stretches over whatever height the text comes to.
        /// </summary>
        public GUIStyle Header => _header ??= new GUIStyle
        {
            normal = {background = Fill(HeaderFill)},
            padding = new RectOffset(0, 0, 0, 0),
            margin = new RectOffset(0, 0, 0, 0)
        };

        /// <summary>
        /// The textures the styles above are built from. They are not saved with the window and
        /// nothing else refers to them, so the window destroys them when it closes rather than
        /// leaving one behind every time it is opened.
        /// </summary>
        public void Dispose()
        {
            foreach (Texture2D texture in _fills)
            {
                if (texture != null)
                    Object.DestroyImmediate(texture);
            }

            _fills.Clear();
        }

        private Texture2D Fill(Color color)
        {
            var texture = new Texture2D(1, 1) {hideFlags = HideFlags.HideAndDontSave};

            texture.SetPixel(0, 0, color);
            texture.Apply();

            _fills.Add(texture);

            return texture;
        }

        public GUIStyle SidebarRow => _sidebarRow ??= new GUIStyle(GUIStyle.none)
        {
            fixedHeight = 0f,
            padding = new RectOffset(0, 0, 0, 0),
            margin = new RectOffset(0, 0, 0, 0),
            stretchWidth = true
        };
    }
}

#endif