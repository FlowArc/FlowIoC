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
        /// Width kept clear for the sidebar's vertical scrollbar. The bar itself only appears
        /// when the list is taller than the view, but the space is held either way, so a list
        /// that grows past the bottom does not narrow every row the moment the bar arrives.
        /// </summary>
        private const float SidebarScrollbarReserve = 14f;

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

        /// <summary>A topic inside a category: shorter than the category above it, and indented.</summary>
        private const float ChildRowHeight = 34f;

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
            window.minSize = new Vector2(1120f, 600f);
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
            window.minSize = new Vector2(1120f, 600f);
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

            // Every category above the opening topic starts folded open, or the page the window
            // opens on would not be on screen. The introduction sits at the top level and is
            // inside none of them, so the sidebar opens closed: Welcome, Wiki, Modules.
            OpenCategoriesTo(_selected);
        }

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
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawSidebar();
                DrawPage();
            }
        }

        private void DrawSidebar()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(SidebarWidth)))
            {
                // GUIStyle.none for the horizontal bar: the rows are as wide as the view, so a
                // horizontal scrollbar would only ever be a stripe along the bottom.
                //
                // The vertical one appears only when the list outgrows the view. The rows are
                // laid out to a width that already excludes the bar, so the moment it arrives
                // nothing rewraps and the list does not shift under the reader's cursor.
                using (EditorGUILayout.ScrollViewScope sidebar = new EditorGUILayout.ScrollViewScope(
                           _sidebarScroll, false, false,
                           GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none))
                {
                    _sidebarScroll = sidebar.scrollPosition;

                    float rowWidth = SidebarWidth
                                     - EditorStyles.helpBox.padding.horizontal
                                     - SidebarScrollbarReserve;

                    using (new EditorGUILayout.VerticalScope(GUILayout.Width(rowWidth)))
                    {
                        foreach (HelpSection section in _catalog.Sections)
                            DrawSection(section, string.Empty, 0);
                    }

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
            float indent = depth * SidebarIndent;

            if (!section.IsCategory)
            {
                // The gloss under a title is only worth its line on a top level entry, which has
                // the height for it. Inside a category the rows are short, and a subtitle there
                // wraps the name onto a second line to make room for a third.
                string label = depth == 0 ? Label(section.Title, section.Subtitle) : section.Title;

                if (DrawRow(label, section.Icon, section.Featured,
                        _selected == section.Page, height, indent, null))
                {
                    Select(section.Page);
                }

                return;
            }

            string key = KeyOf(parentPath, section);
            bool open = _openCategories.Contains(key);

            if (DrawRow(section.Title, section.Icon, false, false, height, indent, open))
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
        /// One sidebar row. The icon and the text are placed by hand rather than handed to the
        /// button as a GUIContent, because the built-in icons come in different sizes and letting
        /// the style lay them out leaves every row starting somewhere else.
        /// </summary>
        private bool DrawRow(string label, string icon, bool featured, bool active, float height,
            float indent, bool? expanded)
        {
            Color previous = GUI.backgroundColor;

            if (featured)
                GUI.backgroundColor = _theme.Banner;

            bool pressed;

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(indent);
                pressed = GUILayout.Toggle(active, GUIContent.none, _theme.SidebarButton,
                    GUILayout.Height(height));
            }

            GUI.backgroundColor = previous;

            Rect row = GUILayoutUtility.GetLastRect();

            if (Event.current.type != EventType.Repaint)
                return pressed && !active;

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

            GUI.Label(textRect, label, _theme.SidebarLabel);

            if (expanded.HasValue)
            {
                Rect arrowRect = new Rect(row.xMax - SidebarPadding - arrowWidth, row.y,
                    arrowWidth, row.height);

                GUI.Label(arrowRect, expanded.Value ? "▾" : "▸", _theme.SidebarLabel);
            }

            return pressed && !active;
        }

        private string Label(string title, string subtitle) =>
            string.IsNullOrEmpty(subtitle) ? title : title + "\n" + subtitle;

        private void DrawPage()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                // The banner is drawn outside the scroll view, so the page title and its tabs stay
                // put while the reader scrolls the body under them.
                _selected.SelectedTab = _painter.Banner(_selected.Title, _selected.Tabs,
                    _selected.SelectedTab, _selected.Action);

                using (EditorGUILayout.ScrollViewScope scroll = new EditorGUILayout.ScrollViewScope(_scroll))
                {
                    _scroll = scroll.scrollPosition;

                    using (new EditorGUILayout.VerticalScope())
                    {
                        GUILayout.Space(4f);
                        _selected.Draw(_painter);
                        GUILayout.Space(12f);
                    }
                }
            }
        }
    }
}

#endif