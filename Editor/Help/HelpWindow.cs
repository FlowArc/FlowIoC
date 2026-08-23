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
        private const float SidebarWidth = 180f;

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

        [MenuItem("Tools/FlowIoC/Help", false, 200)]
        internal static void Open()
        {
            HelpWindow window = GetWindow<HelpWindow>("FlowIoC Help");
            window.minSize = new Vector2(900f, 560f);
            window.Show();
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
        }

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
                using (EditorGUILayout.ScrollViewScope sidebar = new EditorGUILayout.ScrollViewScope(
                           _sidebarScroll, false, false,
                           GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none))
                {
                    _sidebarScroll = sidebar.scrollPosition;

                    foreach (HelpSection section in _catalog.Sections)
                        DrawSection(section);

                    GUILayout.FlexibleSpace();
                }
            }
        }

        private void DrawSection(HelpSection section)
        {
            if (!section.IsCategory)
            {
                if (DrawRow(Label(section.Title, section.Subtitle), section.Icon, section.Featured,
                        _selected == section.Page, SidebarButtonHeight, 0f, null))
                {
                    Select(section.Page);
                }

                return;
            }

            bool open = _openCategories.Contains(section.Title);

            if (DrawRow(section.Title, section.Icon, false, false, SidebarButtonHeight, 0f, open))
            {
                if (open)
                    _openCategories.Remove(section.Title);
                else
                    _openCategories.Add(section.Title);

                GUI.FocusControl(null);
            }

            if (!open)
                return;

            foreach (IHelpPage page in section.Pages)
            {
                if (DrawRow(page.Title, page.Icon, false, _selected == page, ChildRowHeight,
                        SidebarIndent, null))
                {
                    Select(page);
                }
            }
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
                _selected.SelectedTab = _painter.Banner(_selected.Title, _selected.Tabs, _selected.SelectedTab);

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