#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.Help.Graph;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.Help
{
    /// <summary>
    /// Every mark the help window makes. Pages call these and nothing else, which is what keeps
    /// eight pages looking like one window.
    ///
    /// The class is public because a private module in another package writes its page against
    /// it. Only the marks are public: the constructor, the banner, the tree and the graph take
    /// types the package does not publish and belong to the window rather than to a page.
    /// </summary>
    public class HelpPainter
    {
        private readonly HelpTheme _theme;
        private readonly HelpGraphPainter _graphPainter;

        internal HelpPainter(HelpTheme theme)
        {
            _theme = theme;
            _graphPainter = new HelpGraphPainter(theme);
        }

        /// <summary>
        /// The purple bar every page wears: its title on the left, and the readings it offers as
        /// buttons on the right. The window draws this outside the scroll view, so the title and
        /// the tabs stay put while the page scrolls under them.
        /// </summary>
        internal int Banner(string title, IReadOnlyList<HelpTab> tabs, int selected, HelpAction action = null)
        {
            Color previous = GUI.backgroundColor;
            GUI.backgroundColor = _theme.Banner;

            int chosen = selected;

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox,
                       GUILayout.Height(_theme.BannerHeight)))
            {
                GUILayout.Label(EditorGUIUtility.IconContent("console.infoicon"),
                    GUILayout.Width(35f), GUILayout.Height(_theme.BannerHeight));

                EditorGUILayout.LabelField(title, _theme.Heading,
                    GUILayout.Height(_theme.BannerHeight), GUILayout.ExpandWidth(false));

                GUILayout.FlexibleSpace();

                if (tabs != null && tabs.Count > 1)
                {
                    // The buttons are drawn in the ordinary button colour: purple on purple would
                    // leave them invisible against the bar they sit in.
                    GUI.backgroundColor = previous;

                    var titles = new string[tabs.Count];

                    for (int index = 0; index < tabs.Count; index++)
                        titles[index] = tabs[index].Title;

                    chosen = GUILayout.Toolbar(selected, titles, _theme.BannerTab,
                        GUILayout.Height(_theme.BannerTabHeight),
                        GUILayout.Width(_theme.BannerTabWidth * titles.Length));

                    GUI.backgroundColor = _theme.Banner;
                }

                DrawAction(action);
            }

            GUI.backgroundColor = previous;

            return chosen;
        }

        /// <summary>
        /// The page's own action, at the far right of the banner. It is drawn last so it sits
        /// outside the tabs: the tabs change what you are reading, this changes the project.
        /// </summary>
        private void DrawAction(HelpAction action)
        {
            if (action == null)
                return;

            GUILayout.Space(8f);

            Color previous = GUI.backgroundColor;
            GUI.backgroundColor = _theme.Action;

            using (new EditorGUI.DisabledScope(!action.Enabled))
            {
                if (GUILayout.Button(action.Label, _theme.ActionButton,
                        GUILayout.Width(_theme.ActionWidth),
                        GUILayout.Height(_theme.ActionHeight)))
                {
                    action.Perform();
                }
            }

            GUI.backgroundColor = previous;
        }

        public void SubHeading(string text) => EditorGUILayout.LabelField(text, _theme.SubHeading);

        public void Paragraph(string text) => EditorGUILayout.LabelField(text, _theme.Body);

        public void Bullet(string text)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(6f);
                EditorGUILayout.LabelField("•", GUILayout.Width(10f));
                EditorGUILayout.LabelField(text, _theme.Body);
            }
        }

        public void Rule(string text) => EditorGUILayout.LabelField(text, _theme.Rule);

        public void Note(string text) => EditorGUILayout.HelpBox(text, MessageType.Info);

        public void Space() => EditorGUILayout.Space();

        public void Code(string code)
        {
            if (string.IsNullOrEmpty(code))
                return;

            float height = _theme.Code.CalcHeight(new GUIContent(code), EditorGUIUtility.currentViewWidth) + 4f;

            EditorGUILayout.SelectableLabel(code, _theme.Code, GUILayout.Height(height));
        }

        /// <summary>
        /// A screenshot, scaled down to the page width when it is wider and left at its own size
        /// when it is not - an editor window blown up past its pixels reads as a blurred mistake.
        /// A picture the project no longer ships draws nothing rather than a magenta rectangle:
        /// the page is still worth reading without it.
        /// </summary>
        public void Image(Texture2D image, string caption = null)
        {
            if (image == null)
                return;

            float available = EditorGUIUtility.currentViewWidth - _theme.ImageMargin;
            float width = Mathf.Min(image.width, available);
            float height = width * image.height / image.width;

            Rect rect = GUILayoutUtility.GetRect(width, height, GUILayout.ExpandWidth(false));

            if (Event.current.type == EventType.Repaint)
            {
                GUI.DrawTexture(rect, image, ScaleMode.ScaleToFit);

                Handles.BeginGUI();
                Handles.color = _theme.ImageBorder;
                Handles.DrawAAPolyLine(1.5f,
                    new Vector3(rect.xMin, rect.yMin), new Vector3(rect.xMax, rect.yMin),
                    new Vector3(rect.xMax, rect.yMax), new Vector3(rect.xMin, rect.yMax),
                    new Vector3(rect.xMin, rect.yMin));
                Handles.EndGUI();
            }

            if (!string.IsNullOrEmpty(caption))
                EditorGUILayout.LabelField(caption, _theme.Caption, GUILayout.Width(width));

            Space();
        }

        internal void Tree(HelpTreeNode root) => DrawTree(root, 0);

        internal void Graph(HelpGraph graph, HelpGraphStepper stepper)
        {
            if (graph == null)
                return;

            _graphPainter.Draw(graph, stepper);

            if (stepper.Count == 0)
                return;

            HelpGraphStep step = graph.Steps[stepper.Index];

            Rule(step.Rule);
            Code(step.Code);
        }

        private void DrawTree(HelpTreeNode node, int depth)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(depth * 14f);

                float nameWidth = Mathf.Max(90f, 200f - depth * 14f);
                EditorGUILayout.LabelField(node.Name, _theme.TreeName, GUILayout.Width(nameWidth));

                if (string.IsNullOrEmpty(node.Comment))
                {
                    GUILayout.FlexibleSpace();
                }
                else
                {
                    Color previous = GUI.color;
                    GUI.color = _theme.MutedText;
                    EditorGUILayout.LabelField(node.Comment, _theme.TreeComment);
                    GUI.color = previous;
                }
            }

            foreach (HelpTreeNode child in node.Children)
                DrawTree(child, depth + 1);
        }
    }
}

#endif