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
    /// </summary>
    internal class HelpPainter
    {
        private readonly HelpTheme _theme;
        private readonly HelpGraphPainter _graphPainter;

        public HelpPainter(HelpTheme theme)
        {
            _theme = theme;
            _graphPainter = new HelpGraphPainter(theme);
        }

        /// <summary>
        /// The purple bar every page wears: its title on the left, and the readings it offers as
        /// buttons on the right. The window draws this outside the scroll view, so the title and
        /// the tabs stay put while the page scrolls under them.
        /// </summary>
        public int Banner(string title, IReadOnlyList<HelpTab> tabs, int selected)
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
            }

            GUI.backgroundColor = previous;

            return chosen;
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

        public void Tree(HelpTreeNode root) => DrawTree(root, 0);

        public void Graph(HelpGraph graph, HelpGraphStepper stepper)
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