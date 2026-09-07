#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.Help
{
    /// <summary>
    /// An introduction to FlowIoC that lives in the Editor. The window teaches one worked example
    /// and never inspects the project it is opened in, so every project sees the same pages.
    /// </summary>
    internal class HelpWindow : EditorWindow
    {
        /// <summary>
        /// Wide enough that a topic two categories deep still fits its name on one line. The
        /// indent eats into the text on every level, so the width is set by the deepest row
        /// rather than by the top one.
        /// </summary>
        private const float SidebarWidth = 240f;

        /// <summary>
        /// Three times the height of an ordinary mini button, so a topic reads as a place to go
        /// rather than as a row in a list.
        /// </summary>
        private const float SidebarButtonHeight = 54f;

        /// <summary>
        /// One box for every icon whatever its own resolution, so the icons line up down the
        /// column and every title starts at the same x.
        /// </summary>
        private const float SidebarIconSize = 22f;

        private const float SidebarPadding = 8f;

        /// <summary>
        /// What the groove between two rows takes off the bottom of the upper one: a dark hairline
        /// and a light one under it.
        /// </summary>
        private const float SidebarGrooveHeight = 2f;

        /// <summary>The line down the right of the panel, which the rows stop short of.</summary>
        private const float SidebarBorderWidth = 1f;

        /// <summary>
        /// What the page keeps clear above the headline and above its first paragraph. The margin
        /// at the left and the right is the theme's, because a page's own marks reach past it.
        /// </summary>
        private const float PageTopPadding = 14f;

        /// <summary>The hairline the header band ends on.</summary>
        private const float HeaderEdgeHeight = 1f;

        /// <summary>
        /// How tall the strip of readings at the foot of the band is, and how much of the selected
        /// tab is given to the violet line that marks it.
        /// </summary>
        private const float TabStripHeight = 26f;

        private const float TabAccentHeight = 2f;

        /// <summary>
        /// How wide and how tall a category's triangle is drawn. Odin's own is eight pixels across
        /// with a pixel of it given to the smoothing at each edge, so seven is what is left for the
        /// shape itself and drawing eight leaves an arrow visibly bigger than the one next door.
        /// </summary>
        private const float SidebarArrowSize = 7f;

        /// <summary>
        /// The smallest the window may be. Wide enough that the screenshots the pages ship draw
        /// at their own pixels: an editor window resampled to fit is a blurred editor window, and
        /// the widest picture here is the Screen Scanner at 880.
        /// </summary>
        private static readonly Vector2 MinimumSize = new Vector2(1200f, 600f);

        /// <summary>
        /// A topic inside a category: shorter than the category above it, and indented. Half the
        /// height of a top level entry, because that entry carries a second line of gloss and this
        /// one carries a name - and a menu of names reads faster the closer together they sit.
        /// </summary>
        private const float ChildRowHeight = 28f;

        private const float SidebarIndent = 12f;

        /// <summary>
        /// One menu entry per top level section, so a reader picks what they are here for before
        /// the window is even up. There is deliberately no plain Help entry: Unity draws a
        /// submenu or an item at a path, never both, and three named ways in beat one that always
        /// lands on the introduction.
        /// </summary>
        [MenuItem("Tools/FlowIoC/Help/Welcome", false, -1100)]
        private static void OpenWelcome() => Open("Welcome");

        [MenuItem("Tools/FlowIoC/Help/Wiki", false, -1099)]
        private static void OpenWiki() => Open("Wiki");

        [MenuItem("Tools/FlowIoC/Help/Modules", false, -1098)]
        private static void OpenModules() => Open("Modules");

        internal static void Open() => Open(null);

        /// <summary>
        /// Opens the window on the first topic of <paramref name="sectionTitle"/>. The selection
        /// is made here rather than in OnEnable, because a window that is already open does not
        /// run OnEnable again and would otherwise ignore which entry was clicked.
        /// </summary>
        internal static void Open(string sectionTitle)
        {
            HelpWindow window = GetWindow<HelpWindow>("FlowIoC Help");
            window.minSize = MinimumSize;
            window.Show();

            if (!string.IsNullOrEmpty(sectionTitle))
                window.GoTo(sectionTitle);
        }

        /// <summary>
        /// Opens the window on one exact topic. The header bar of an inspector links here, so the
        /// reader lands on the page for what they were looking at rather than on the section it
        /// happens to sit in.
        /// </summary>
        internal static void OpenPage(string pageTitle) => OpenPage(pageTitle, null);

        /// <summary>
        /// Opens the window on one topic and one of its readings. The startup notice asks for
        /// Welcome's What's New this way, so that landing on a tab needs no knowledge of which
        /// number that tab happens to be.
        /// </summary>
        internal static void OpenPage(string pageTitle, string tabTitle)
        {
            HelpWindow window = GetWindow<HelpWindow>("FlowIoC Help");
            window.minSize = MinimumSize;
            window.Show();

            window.GoToPage(pageTitle, tabTitle);
        }

        private void GoToPage(string pageTitle, string tabTitle)
        {
            IHelpPage page = _catalog.FindPage(pageTitle);

            if (page == null)
                return;

            Select(page);
            SelectTab(page, tabTitle);
            OpenCategoriesTo(page);
            Repaint();
        }

        /// <summary>
        /// A page keeps the reading it was left on, so a tab that was asked for is set and a
        /// caller that named none leaves the page as the reader had it. A title the page does
        /// not offer changes nothing.
        /// </summary>
        private void SelectTab(IHelpPage page, string tabTitle)
        {
            if (string.IsNullOrEmpty(tabTitle))
                return;

            for (var i = 0; i < page.Tabs.Count; i++)
            {
                if (page.Tabs[i].Title != tabTitle) continue;

                page.SelectedTab = i;

                return;
            }
        }

        /// <summary>
        /// Selects the section's first topic and folds open every category above it, so the
        /// sidebar shows where the reader has landed rather than a closed tree.
        /// </summary>
        private void GoTo(string sectionTitle)
        {
            IHelpPage page = _catalog.FirstPageOf(sectionTitle);

            if (page == null)
                return;

            Select(page);
            OpenCategoriesTo(page);
            Repaint();
        }

        private HelpPageCatalog _catalog;
        private HelpTheme _theme;
        private HelpPainter _painter;
        private IHelpPage _selected;
        private readonly HashSet<string> _openCategories = new HashSet<string>();
        private Vector2 _scroll;
        private Vector2 _sidebarScroll;

        private void OnEnable()
        {
            _catalog = new HelpPageCatalog();
            _theme = new HelpTheme();
            _painter = new HelpPainter(_theme);
            _selected = _catalog.OpeningPage;
            _openCategories.Clear();

            // The sidebar rows light up under the pointer, and a highlight that only arrives when
            // something else forces a repaint is worse than none at all.
            wantsMouseMove = true;

            // Every category above the opening topic starts folded open, or the page the window
            // opens on would not be on screen. The introduction sits at the top level and is
            // inside none of them, so the sidebar opens closed: Welcome, Wiki, Modules.
            OpenCategoriesTo(_selected);
        }

        /// <summary>The theme holds textures of its own, and they go when the window does.</summary>
        private void OnDisable() => _theme?.Dispose();

        private void OpenCategoriesTo(IHelpPage page)
        {
            var path = string.Empty;

            foreach (HelpSection category in _catalog.CategoriesContaining(page))
            {
                path = KeyOf(path, category);
                _openCategories.Add(path);
            }
        }

        /// <summary>
        /// What the open categories are remembered by. Two categories may share a title once
        /// modules bring categories of their own, so the path down to one is the key rather than
        /// its name.
        /// </summary>
        private static string KeyOf(string parentPath, HelpSection section) =>
            string.IsNullOrEmpty(parentPath) ? section.Title : parentPath + "/" + section.Title;

        private void OnGUI()
        {
            // The sidebar's own fill, painted before anything is laid out on top of it. The panel
            // is the full height of the window whatever the list inside it comes to, and a fill
            // drawn from the layout would stop where the rows do.
            if (Event.current.type == EventType.Repaint)
            {
                // The page is painted as well as the panel, because the Editor's own window grey is
                // the middle of the range this window uses: leaving the page on it would put the
                // page and the band above it a hair apart instead of a step.
                EditorGUI.DrawRect(new Rect(SidebarWidth, 0f, position.width - SidebarWidth, position.height),
                    _theme.PageFill);

                EditorGUI.DrawRect(new Rect(0f, 0f, SidebarWidth, position.height), _theme.SidebarFill);

                EditorGUI.DrawRect(
                    new Rect(SidebarWidth - SidebarBorderWidth, 0f, SidebarBorderWidth, position.height),
                    _theme.SidebarBorder);
            }

            // A moved pointer changes which row is lit, and Unity delivers the move without
            // repainting on its own, so the window asks for the repaint the highlight needs.
            if (Event.current.type == EventType.MouseMove)
                Repaint();

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawSidebar();

                // The line between the two is painted, not laid out, so the page has to be moved
                // over by its width. Without this the page starts where the line is and covers it,
                // and the panel loses its edge everywhere the page happens to paint a fill.
                GUILayout.Space(SidebarBorderWidth);

                DrawPage();
            }
        }

        private void DrawSidebar()
        {
            // Everything but the line that closes the panel off. The border is painted before the
            // rows are laid out, so the rows are given the width that is left rather than drawing
            // over it.
            using (new EditorGUILayout.VerticalScope(GUIStyle.none,
                       GUILayout.Width(SidebarWidth - SidebarBorderWidth)))
            {
                // GUIStyle.none for the horizontal bar: the rows are as wide as the view, so a
                // horizontal scrollbar would only ever be a stripe along the bottom.
                //
                // The vertical one appears only when the list outgrows the view, and nothing is
                // held clear for it in the meantime: a row is drawn to whatever the view is wide,
                // so the menu is filled to both its edges until the day the bar actually arrives
                // and takes its own strip. A gutter kept empty against that day is a gutter the
                // reader looks at every other day.
                using (EditorGUILayout.ScrollViewScope sidebar = new EditorGUILayout.ScrollViewScope(
                           _sidebarScroll, false, false,
                           GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none))
                {
                    _sidebarScroll = sidebar.scrollPosition;

                    foreach (HelpSection section in _catalog.Sections)
                        DrawSection(section, string.Empty, 0);

                    GUILayout.FlexibleSpace();
                }
            }
        }

        /// <summary>
        /// One sidebar entry and, when it is an open category, everything under it. Depth decides
        /// how far the row is indented and how tall it is: a top level entry reads as a place to
        /// go, and anything inside a category is a shorter row set in from it.
        /// </summary>
        private void DrawSection(HelpSection section, string parentPath, int depth)
        {
            float height = depth == 0 ? SidebarButtonHeight : ChildRowHeight;

            if (!section.IsCategory)
            {
                // The gloss under a title is only worth its line on a top level entry, which has
                // the height for it. Inside a category the rows are short, and a subtitle there
                // wraps the name onto a second line to make room for a third.
                string label = depth == 0 ? Label(section.Title, section.Subtitle) : section.Title;

                if (DrawRow(label, section.Icon, section.Featured,
                        _selected == section.Page, height, depth, null))
                {
                    Select(section.Page);
                }

                return;
            }

            string key = KeyOf(parentPath, section);
            bool open = _openCategories.Contains(key);

            if (DrawRow(section.Title, section.Icon, false, false, height, depth, open))
            {
                if (open)
                    _openCategories.Remove(key);
                else
                    _openCategories.Add(key);

                GUI.FocusControl(null);
            }

            if (!open)
                return;

            foreach (HelpSection child in section.Children)
                DrawSection(child, key, depth + 1);
        }

        private void Select(IHelpPage page)
        {
            _selected = page;
            _scroll = Vector2.zero;
            GUI.FocusControl(null);
        }

        /// <summary>
        /// One sidebar row. The row itself is a rectangle that takes the click and draws nothing:
        /// the fill, the hairline under it, the icon and the text are all placed by hand. The icons
        /// are the reason for the last two - the built-in ones come in different sizes, and letting
        /// a style lay them out leaves every row starting somewhere else - and the fill is the
        /// reason for the first: a row is filled to both edges of the panel, which a button drawn
        /// inside its own margins cannot be.
        ///
        /// The indent moves what is drawn in the row rather than the row, so a topic two categories
        /// deep is still highlighted the full width of the menu.
        /// </summary>
        private bool DrawRow(string label, string icon, bool featured, bool active, float height,
            int depth, bool? expanded)
        {
            bool pressed = GUILayout.Toggle(active, GUIContent.none, _theme.SidebarRow,
                GUILayout.Height(height), GUILayout.ExpandWidth(true));

            Rect row = GUILayoutUtility.GetLastRect();

            if (Event.current.type != EventType.Repaint)
                return pressed && !active;

            float indent = depth * SidebarIndent;

            DrawRowBackground(row, featured, active, depth);

            float iconSize = height < SidebarButtonHeight ? SidebarIconSize - 4f : SidebarIconSize;
            Texture texture = string.IsNullOrEmpty(icon)
                ? null
                : EditorGUIUtility.IconContent(icon).image;

            if (texture != null)
            {
                Rect iconRect = new Rect(row.x + indent + SidebarPadding,
                    row.y + (row.height - iconSize) * 0.5f, iconSize, iconSize);

                GUI.DrawTexture(iconRect, texture, ScaleMode.ScaleToFit);
            }

            float arrowWidth = expanded.HasValue ? 14f : 0f;
            float textX = row.x + indent + SidebarPadding + SidebarIconSize + SidebarPadding;
            Rect textRect = new Rect(textX, row.y,
                row.xMax - SidebarPadding - arrowWidth - textX, row.height);

            GUIStyle text = active ? _theme.SidebarLabelActive : _theme.SidebarLabel;

            GUI.Label(textRect, label, text);

            if (expanded.HasValue)
            {
                Rect arrowRect = new Rect(row.xMax - SidebarPadding - arrowWidth, row.y,
                    arrowWidth, row.height);

                DrawArrow(arrowRect, expanded.Value,
                    active ? _theme.SidebarArrowActive : _theme.SidebarArrow);
            }

            return pressed && !active;
        }

        /// <summary>
        /// The triangle at the right of a category: pointing right while it is shut and down while
        /// it is open. It is neither a character nor a stack of rectangles - the glyph a font gives
        /// for an arrow is a fraction of the line it sits on and reads as a speck beside the name,
        /// and a stack of rectangles is the size it is asked to be but shows every step of its
        /// diagonal. Handles draw the three corners with the edges smoothed, which is the same
        /// picture Odin's own menu paints and the reason its arrows look drawn rather than plotted.
        /// </summary>
        private void DrawArrow(Rect area, bool expanded, Color color)
        {
            float x = area.x + (area.width - SidebarArrowSize) * 0.5f;
            float y = area.y + (area.height - SidebarArrowSize) * 0.5f;

            Vector3[] corners = expanded
                ? new[]
                {
                    new Vector3(x, y + 1f),
                    new Vector3(x + SidebarArrowSize, y + 1f),
                    new Vector3(x + SidebarArrowSize * 0.5f, y + SidebarArrowSize)
                }
                : new[]
                {
                    new Vector3(x + 1f, y),
                    new Vector3(x + SidebarArrowSize, y + SidebarArrowSize * 0.5f),
                    new Vector3(x + 1f, y + SidebarArrowSize)
                };

            Color previous = Handles.color;
            Handles.color = color;

            Handles.DrawAAConvexPolygon(corners);

            Handles.color = previous;
        }

        /// <summary>
        /// What a row is filled with, and the hairline that ends it. The panel's own colour a shade
        /// darker for every category above the row, so a fold that opens steps down into the panel
        /// rather than adding more of the same surface. Over that, the selected row carries the
        /// window's own violet and a featured one a tint of the same colour, so the two read as
        /// degrees of one thing rather than as two unrelated marks. The pointer lights up whatever
        /// it is over, because every row here does something when it is clicked.
        /// </summary>
        private void DrawRowBackground(Rect row, bool featured, bool active, int depth)
        {
            // Everything above the groove. A fill drawn over the two hairlines would close the
            // step between two rows, and the list would go back to being one flat column.
            var fill = new Rect(row.x, row.y, row.width, row.height - SidebarGrooveHeight);

            if (depth > 0)
                EditorGUI.DrawRect(fill, _theme.SidebarRowFill(depth));

            if (active)
            {
                EditorGUI.DrawRect(fill, _theme.SidebarRowSelected);
                EditorGUI.DrawRect(new Rect(fill.x, fill.y, fill.width, 1f), _theme.SidebarRowSelectedTop);
                EditorGUI.DrawRect(new Rect(fill.x, fill.yMax - 1f, fill.width, 1f),
                    _theme.SidebarRowSelectedBottom);
            }
            else if (featured)
            {
                EditorGUI.DrawRect(fill, _theme.SidebarRowFeatured);
            }

            if (!active && row.Contains(Event.current.mousePosition))
                EditorGUI.DrawRect(fill, _theme.SidebarRowHover);

            EditorGUI.DrawRect(new Rect(row.x, row.yMax - 2f, row.width, 1f), _theme.SidebarSeparator);
            EditorGUI.DrawRect(new Rect(row.x, row.yMax - 1f, row.width, 1f), _theme.SidebarSeparatorLight);
        }

        private string Label(string title, string subtitle) =>
            string.IsNullOrEmpty(subtitle) ? title : title + "\n" + subtitle;

        private void DrawPage()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                // The banner and the band under it are drawn outside the scroll view, so the title,
                // what the page is about and the readings it offers all stay put while the body
                // scrolls under them.
                _painter.Banner(_selected.Title, _selected.Action);

                int previous = _selected.SelectedTab;
                int chosen = DrawHeader();

                _selected.SelectedTab = chosen;

                // A new reading starts at its own beginning. Carrying the scroll position across
                // drops the reader into the middle of a page they have not read a line of.
                if (chosen != previous)
                {
                    _scroll = Vector2.zero;
                    GUI.FocusControl(null);
                }

                using (EditorGUILayout.ScrollViewScope scroll = new EditorGUILayout.ScrollViewScope(_scroll))
                {
                    _scroll = scroll.scrollPosition;

                    // The page is inset further than the band above it, so the reading starts a
                    // step in from the headline that names it rather than under the same margin.
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Space(_theme.PageBodyPadding);

                        using (new EditorGUILayout.VerticalScope())
                        {
                            GUILayout.Space(PageTopPadding);
                            _selected.Draw(_painter);
                            GUILayout.Space(12f);
                        }

                        GUILayout.Space(_theme.PageBodyPadding);
                    }
                }
            }
        }

        /// <summary>
        /// The band under the banner: what this reading of the page opens with, and the paragraph
        /// below it. It is drawn outside the scroll view because it says what the reader is looking
        /// at, which is the last thing that should scroll away - and a reading that names no
        /// headline falls back to the page's own title, so the band is never empty.
        /// </summary>
        private int DrawHeader()
        {
            HelpTab tab = _selected.Tabs[_selected.SelectedTab];

            string headline = string.IsNullOrEmpty(tab.Headline) ? _selected.Title : tab.Headline;

            using (new EditorGUILayout.HorizontalScope(_theme.Header, GUILayout.Height(BandHeight())))
            {
                GUILayout.Space(_theme.PagePadding);

                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Space(PageTopPadding);

                    EditorGUILayout.LabelField(headline, _theme.Hero);

                    if (!string.IsNullOrEmpty(tab.Tagline))
                    {
                        // The paragraph is drawn muted, so the headline above it is read first and
                        // the two still read as one opening rather than as two paragraphs.
                        Color previous = GUI.color;
                        GUI.color = _theme.MutedText;

                        EditorGUILayout.LabelField(tab.Tagline, _theme.HeroTagline);

                        GUI.color = previous;
                    }

                    // What is left of the band once this reading has had its lines. The height is
                    // the page's rather than the reading's, so the space below the text is what
                    // varies from tab to tab instead of the strip's position.
                    GUILayout.FlexibleSpace();
                }

                GUILayout.Space(_theme.PagePadding);
            }

            int chosen = DrawTabs();

            // The strip closes the band itself, one tab at a time, so that the open tab has no line
            // under it. A page with a single reading draws no strip, and the band ends on its own.
            if (_selected.Tabs.Count < 2)
                DrawHeaderEdge();

            return chosen;
        }

        /// <summary>
        /// How tall the band is: whatever the longest of this page's readings needs, rather than
        /// whatever the open one needs. A band that shrinks when a shorter headline is chosen takes
        /// the strip of tabs up with it, and a tab that moves when it is clicked stops reading as a
        /// row of tabs at all.
        ///
        /// It is measured rather than fixed, because a tagline wraps to two lines in a wide window
        /// and to four in a narrow one, and a height set once would clip the narrow case.
        /// </summary>
        private float BandHeight()
        {
            float width = position.width - SidebarWidth - SidebarBorderWidth - _theme.PagePadding * 2f;
            var tallest = 0f;

            foreach (HelpTab tab in _selected.Tabs)
            {
                string headline = string.IsNullOrEmpty(tab.Headline) ? _selected.Title : tab.Headline;

                float height = _theme.Hero.CalcHeight(new GUIContent(headline), width)
                               + _theme.Hero.margin.vertical;

                if (!string.IsNullOrEmpty(tab.Tagline))
                {
                    height += _theme.HeroTagline.CalcHeight(new GUIContent(tab.Tagline), width)
                              + _theme.HeroTagline.margin.vertical;
                }

                tallest = Mathf.Max(tallest, height);
            }

            return tallest + PageTopPadding * 2f;
        }

        /// <summary>
        /// The readings this page offers, as a strip along the foot of the band. They are equal
        /// widths across the whole page rather than buttons gathered at one end, which is what puts
        /// a tab directly on top of what it opens: the selected one is filled in the page's own
        /// colour, so it reads as the top edge of the page rather than as a pressed button.
        ///
        /// A page with one reading draws no strip at all - a single tab is a label that looks like
        /// a control.
        /// </summary>
        private int DrawTabs()
        {
            IReadOnlyList<HelpTab> tabs = _selected.Tabs;

            if (tabs.Count < 2)
                return _selected.SelectedTab;

            int chosen = _selected.SelectedTab;

            Rect strip = GUILayoutUtility.GetRect(0f, TabStripHeight, GUILayout.ExpandWidth(true));
            float width = strip.width / tabs.Count;

            for (var index = 0; index < tabs.Count; index++)
            {
                var cell = new Rect(strip.x + index * width, strip.y, width, strip.height);
                bool active = index == chosen;

                if (Event.current.type == EventType.Repaint)
                    DrawTab(cell, tabs[index].Title, active, index > 0);

                // The whole cell takes the click, and the label is drawn into it above rather than
                // handed to a button, so the tab is the width it was given whatever its name is.
                if (GUI.Button(cell, GUIContent.none, GUIStyle.none) && !active)
                    chosen = index;
            }

            return chosen;
        }

        private void DrawTab(Rect cell, string title, bool active, bool divided)
        {
            // A tab that is not open is the band it hangs from, so the strip carries no colour of
            // its own: what tells one tab from the next is the dark line between them, and what
            // says which is open is the fill of the page showing through it.
            EditorGUI.DrawRect(cell, active ? _theme.PageFill : _theme.HeaderFill);

            if (!active && cell.Contains(Event.current.mousePosition))
                EditorGUI.DrawRect(cell, _theme.SidebarRowHover);

            if (active)
            {
                // The window's violet along the top says which reading is open, and the foot of the
                // tab is left bare so it runs into the page under it as one surface.
                EditorGUI.DrawRect(new Rect(cell.x, cell.y, cell.width, TabAccentHeight),
                    _theme.SidebarRowSelected);
            }
            else
            {
                EditorGUI.DrawRect(new Rect(cell.x, cell.yMax - HeaderEdgeHeight, cell.width, HeaderEdgeHeight),
                    _theme.HeaderEdge);
            }

            if (divided)
                EditorGUI.DrawRect(new Rect(cell.x, cell.y, 1f, cell.height), _theme.HeaderEdge);

            GUI.Label(cell, title, active ? _theme.TabLabelActive : _theme.TabLabel);
        }

        /// <summary>
        /// The hairline the header ends on. One pixel, because it only says where the band stops:
        /// the heavy bar is a mark a page makes between two of its own topics, and hanging it here
        /// would leave every page wearing a divider it never asked for.
        /// </summary>
        private void DrawHeaderEdge()
        {
            Rect edge = GUILayoutUtility.GetRect(0f, HeaderEdgeHeight, GUILayout.ExpandWidth(true));

            if (Event.current.type != EventType.Repaint)
                return;

            EditorGUI.DrawRect(edge, _theme.HeaderEdge);
        }
    }
}

#endif