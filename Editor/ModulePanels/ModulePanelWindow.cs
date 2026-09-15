#if UNITY_EDITOR

using System;
using FlowIoC.Editor.Inspector;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.ModulePanels
{
    /// <summary>
    /// The window a <see cref="ModulePanel"/> is drawn in: the house bar in the role's colour,
    /// the panel's rows under it in a scroll view. One window per panel type - opening Local
    /// Save's twice focuses the one that is open - and the panel is rebuilt from its type name
    /// after a domain reload, so a window left open survives a recompile.
    /// </summary>
    public class ModulePanelWindow : EditorWindow
    {
        private const float MIN_WIDTH = 560f;
        private const float MIN_HEIGHT = 320f;
        private const float SIDEBAR_WIDTH = 200f;
        private const float SIDEBAR_BORDER = 1f;

        /// <summary>Which panel this window shows, kept as the name the type is rebuilt from.</summary>
        [SerializeField] private string _panelType;

        private ModulePanel _panel;
        private FlowHeaderBar _bar;
        private FlowRowPainter _rows;
        private FlowPalette _palette;
        private ModulePanelPainter _painter;
        private ModulePanelSidebarPainter _sidebar;
        private Vector2 _scroll;
        private Vector2 _sidebarScroll;

        /// <summary>
        /// Opens the panel, or focuses it when it is already open. A module's menu item is one
        /// line: <c>ModulePanelWindow.Open&lt;LocalSavePanel&gt;()</c>.
        /// </summary>
        public static void Open<TPanel>() where TPanel : ModulePanel, new()
        {
            string typeName = typeof(TPanel).AssemblyQualifiedName;

            foreach (ModulePanelWindow open in Resources.FindObjectsOfTypeAll<ModulePanelWindow>())
            {
                if (open._panelType != typeName)
                    continue;

                open.Focus();
                return;
            }

            var window = CreateInstance<ModulePanelWindow>();
            window._panelType = typeName;
            window.Build();
            window.titleContent = new GUIContent(window._panel.Title);
            window.minSize = new Vector2(MIN_WIDTH, MIN_HEIGHT);
            window.Show();
        }

        /// <summary>
        /// The panel type a window remembers, or null when the name resolves to nothing - the
        /// module was deleted while the window was open, say. Null closes the window rather than
        /// drawing a panel that is not there.
        /// </summary>
        internal static Type PanelTypeOf(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return null;

            Type type = Type.GetType(typeName, false);

            return type != null && typeof(ModulePanel).IsAssignableFrom(type) && !type.IsAbstract ? type : null;
        }

        private void Build()
        {
            Type type = PanelTypeOf(_panelType);

            _panel = type == null ? null : (ModulePanel) Activator.CreateInstance(type);

            if (_panel == null)
                return;

            _palette = new FlowPalette();
            _rows = new FlowRowPainter();
            _bar = new FlowHeaderBar(_palette, new FlowHelpPageMap());
            Color accent = _palette.Accent(_panel.Role, EditorGUIUtility.isProSkin);
            _painter = new ModulePanelPainter(_rows, _palette, accent, _panel.GetType());
            _sidebar = new ModulePanelSidebarPainter(_rows, _palette, accent);
        }

        private void OnGUI()
        {
            if (_panel == null)
                Build();

            if (_panel == null)
            {
                Close();
                return;
            }

            if (Event.current.type == EventType.MouseMove)
                Repaint();

            ModulePanelAction? action = _panel.BarAction;
            _bar.DrawWindow(_panel.Role, _panel.Title, _panel.Module, _panel.Subtitle, action?.Label, action?.OnClick, _panel.HelpPage);

            if (_panel.HasSidebar)
                DrawSplit();
            else
                DrawRows();
        }

        private void DrawRows()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            _panel.Draw(_painter);
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// The list on the left, the rows on the right, each in a scroll view of its own. A scroll
        /// view is a group, so a row that bleeds to x = 0 bleeds to its column's edge and not to
        /// the window's. The sidebar's fill runs the full height under the bar whatever the list
        /// inside it comes to, so it is painted from the window rather than from the rows.
        /// </summary>
        private void DrawSplit()
        {
            if (Event.current.type == EventType.Repaint)
            {
                float top = GUILayoutUtility.GetLastRect().yMax;

                EditorGUI.DrawRect(new Rect(0f, top, SIDEBAR_WIDTH, position.height - top),
                    new Color(0f, 0f, 0f, EditorGUIUtility.isProSkin ? 0.12f : 0.06f));
                EditorGUI.DrawRect(new Rect(SIDEBAR_WIDTH - SIDEBAR_BORDER, top, SIDEBAR_BORDER, position.height - top),
                    new Color(0f, 0f, 0f, 0.35f));
            }

            EditorGUILayout.BeginHorizontal();

            _sidebarScroll = EditorGUILayout.BeginScrollView(_sidebarScroll, GUILayout.Width(SIDEBAR_WIDTH));
            _panel.DrawSidebar(_sidebar);
            EditorGUILayout.EndScrollView();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            _panel.Draw(_painter);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndHorizontal();
        }
    }
}

#endif
