#if UNITY_EDITOR

using System;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.Editor.Help;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.Inspector
{
    /// <summary>
    /// The bar every FlowIoC component wears: what it is, which module it belongs to, and two
    /// ways in - all of the help at once, and the help page for its role.
    ///
    /// The fill is flat. The gradient this replaces built a Texture2D on every repaint, which
    /// leaked one texture per frame and said nothing the colour alone does not.
    /// </summary>
    internal class FlowHeaderBar
    {
        private const float BarHeight = 30f;
        private const float StripHeight = 16f;
        private const float StripeWidth = 3f;
        private const float IconSize = 20f;
        private const float ActionWidth = 70f;
        private const float ActionHeight = 18f;

        private readonly FlowPalette _palette;
        private readonly FlowHelpPageMap _pages;

        private GUIStyle _title;
        private GUIStyle _module;

        public FlowHeaderBar(FlowPalette palette, FlowHelpPageMap pages)
        {
            _palette = palette;
            _pages = pages;
        }

        public void Draw(FlowRole role, string title, string module, string label, string summary, bool helpOpen,
            Action onToggleHelp)
        {
            Rect bar = DrawFrame(role, title, module, label, out Color accent);

            DrawIcons(bar, role, helpOpen, onToggleHelp);

            if (helpOpen && !string.IsNullOrEmpty(summary))
                DrawSummary(summary, accent);

            GUILayout.Space(2f);
        }

        /// <summary>
        /// The same bar for a window. A window has no fields to explain, so it wears no help toggle;
        /// what sits on the right instead is the window's own action - a Refresh, say - beside the
        /// link to the role's help page.
        ///
        /// A window may name that page itself. A window wears a role's colour without being one of
        /// its components, so the page the role sends a component to is not always the page the
        /// window is documented on.
        /// </summary>
        public void DrawWindow(FlowRole role, string title, string module, string label, string actionLabel,
            Action onAction, string helpPage = null)
        {
            bool pro = EditorGUIUtility.isProSkin;

            DrawWindow(_palette.Deep(role), _palette.Accent(role, pro), title, module, label, actionLabel, onAction,
                helpPage ?? _pages.PageFor(role));
        }

        /// <summary>
        /// The bar for a window that wears a colour no role owns - Module Scanner is green because
        /// the rows under it are, and no FlowRole is about a module's health.
        /// </summary>
        public void DrawWindow(Color deep, Color accent, string title, string module, string label,
            string actionLabel, Action onAction, string helpPage)
        {
            Rect bar = DrawFrame(deep, accent, title, module, label);

            float right = DrawHelpPageIcon(bar, helpPage);

            if (!string.IsNullOrEmpty(actionLabel) && onAction != null)
            {
                var actionRect = new Rect(right - ActionWidth, bar.y + 6f, ActionWidth, ActionHeight);

                if (GUI.Button(actionRect, actionLabel, EditorStyles.miniButton))
                    onAction();
            }

            GUILayout.Space(2f);
        }

        private Rect DrawFrame(FlowRole role, string title, string module, string label, out Color accent)
        {
            accent = _palette.Accent(role, EditorGUIUtility.isProSkin);

            return DrawFrame(_palette.Deep(role), accent, title, module, label);
        }

        private Rect DrawFrame(Color deep, Color accent, string title, string module, string label)
        {
            Rect bar = Bleed(EditorGUILayout.GetControlRect(false, BarHeight));
            Rect strip = Bleed(EditorGUILayout.GetControlRect(false, StripHeight));

            EditorGUI.DrawRect(bar, deep);
            EditorGUI.DrawRect(strip, _palette.Strip(deep));
            EditorGUI.DrawRect(new Rect(bar.x, bar.y, StripeWidth, bar.height + strip.height), accent);

            // Upper case, whoever calls. A component's title already arrives that way from the
            // role resolver, and a window that wrote its own would be the odd one out.
            var titleRect = new Rect(bar.x + StripeWidth + 8f, bar.y, bar.width - 60f, bar.height);
            GUI.Label(titleRect, title?.ToUpperInvariant(), TitleStyle());

            var moduleRect = new Rect(strip.x + StripeWidth + 8f, strip.y, strip.width - 16f, strip.height);
            GUI.Label(moduleRect, $"{module} · {label}".ToUpperInvariant(), ModuleStyle(accent));

            return bar;
        }

        /// <summary>
        /// The bar runs the full width of the inspector rather than sitting inside its margins, so
        /// it reads as a header for everything under it instead of as one more field.
        /// </summary>
        private Rect Bleed(Rect rect)
        {
            return new Rect(0f, rect.y, rect.width + rect.x + 4f, rect.height);
        }

        private void DrawIcons(Rect bar, FlowRole role, bool helpOpen, Action onToggleHelp)
        {
            float right = DrawHelpPageIcon(bar, _pages.PageFor(role));

            var toggleRect = new Rect(right - IconSize, bar.y + 5f, IconSize, IconSize);
            var content = new GUIContent(helpOpen ? "▾" : "?", "Show what every field here does");

            if (GUI.Button(toggleRect, content, TitleStyle()))
                onToggleHelp();
        }

        /// <summary>
        /// The link to a help page, and the x the next thing on the right may end at. No page
        /// draws nothing, because an icon opening the wrong page is worse.
        /// </summary>
        private float DrawHelpPageIcon(Rect bar, string page)
        {
            float right = bar.xMax - 6f;

            if (page == null)
                return right;

            var pageRect = new Rect(right - IconSize, bar.y + 5f, IconSize, IconSize);
            var content = new GUIContent(EditorGUIUtility.IconContent("_Help").image, $"Open help: {page}");

            if (GUI.Button(pageRect, content, EditorStyles.label))
                HelpWindow.OpenPage(page);

            return right - IconSize - 4f;
        }

        private void DrawSummary(string summary, Color accent)
        {
            var content = new GUIContent(summary);
            float width = EditorGUIUtility.currentViewWidth - 40f;

            Rect rect = EditorGUILayout.GetControlRect(false,
                EditorStyles.wordWrappedMiniLabel.CalcHeight(content, width) + 8f);

            EditorGUI.DrawRect(rect, new Color(accent.r, accent.g, accent.b, 0.12f));
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, StripeWidth, rect.height), accent);

            var textRect = new Rect(rect.x + StripeWidth + 6f, rect.y + 4f, rect.width - StripeWidth - 12f,
                rect.height - 8f);

            GUI.Label(textRect, content, EditorStyles.wordWrappedMiniLabel);
        }

        private GUIStyle TitleStyle()
        {
            return _title ??= new GUIStyle(EditorStyles.whiteLabel)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = {textColor = _palette.Title}
            };
        }

        private GUIStyle ModuleStyle(Color color)
        {
            _module ??= new GUIStyle(EditorStyles.miniLabel) {alignment = TextAnchor.MiddleLeft};

            // Every state, so the line under the title never brightens as the pointer crosses it.
            // Nothing happens when it is clicked, and text that lights up says otherwise.
            _module.normal.textColor = color;
            _module.hover.textColor = color;
            _module.active.textColor = color;
            _module.focused.textColor = color;

            return _module;
        }
    }
}

#endif