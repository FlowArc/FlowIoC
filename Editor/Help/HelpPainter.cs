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

        /// <summary>
        /// The position a diagram drawn as a map is at: nowhere, because it has no steps. One
        /// instance answers for every such diagram, since none of them can be walked.
        /// </summary>
        private readonly HelpGraphStepper _mapStepper = new HelpGraphStepper(0);

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

        /// <summary>
        /// The one line a page opens with, and the line under it that qualifies it. A reader who
        /// gets no further than this should still leave knowing what the topic is for.
        /// </summary>
        public void Hero(string headline, string tagline = null)
        {
            if (!string.IsNullOrEmpty(headline))
                EditorGUILayout.LabelField(headline, _theme.Hero);

            if (string.IsNullOrEmpty(tagline))
                return;

            Color previous = GUI.color;
            GUI.color = _theme.MutedText;
            EditorGUILayout.LabelField(tagline, _theme.HeroTagline);
            GUI.color = previous;
        }

        /// <summary>
        /// What the topic is made of, as a row of cards across the page. Every card is the height
        /// of the wordiest one, so the row reads as a set rather than as a ragged list, and the
        /// cards share the diagram's fill and hairline so the page speaks one visual language.
        ///
        /// The row fills the page, so the cards line up with the edges of the banner above them
        /// the way every other panel in FlowIoC does. That takes two widths: the height has to be
        /// known before the row is reserved, and a reserved rectangle is only the right width on
        /// repaint. So the height is measured against what the view says is left after the
        /// sidebar - which is never wider than the truth, and so never measures a card too short -
        /// and the cards are then laid out across the rectangle the layout hands back.
        /// </summary>
        public void Parts(params HelpPart[] parts)
        {
            if (parts == null || parts.Length == 0)
                return;

            float gap = _theme.CardGap;
            float gaps = gap * (parts.Length - 1);
            float measured = Mathf.Max(_theme.CardMinWidth,
                (EditorGUIUtility.currentViewWidth - _theme.ContentMargin - gaps) / parts.Length);

            float height = 0f;

            foreach (HelpPart part in parts)
                height = Mathf.Max(height, CardHeight(part, measured));

            Rect row = GUILayoutUtility.GetRect(_theme.CardMinWidth * parts.Length + gaps, height,
                GUILayout.ExpandWidth(true));

            if (Event.current.type == EventType.Repaint)
            {
                float width = Mathf.Max(_theme.CardMinWidth, (row.width - gaps) / parts.Length);

                for (int index = 0; index < parts.Length; index++)
                {
                    Rect rect = new Rect(row.x + index * (width + gap), row.y, width, height);
                    DrawPart(rect, parts[index]);
                }
            }

            Space();
        }

        /// <summary>
        /// A code block, and above it the file the code is from. The caption is worth more than
        /// the same path commented into the snippet's first line, because the snippet then reads
        /// as the file rather than as a comment about one.
        /// </summary>
        public void Code(string code, string caption = null)
        {
            if (string.IsNullOrEmpty(code))
                return;

            if (!string.IsNullOrEmpty(caption))
            {
                Color previous = GUI.color;
                GUI.color = _theme.MutedText;
                EditorGUILayout.LabelField(caption, _theme.CodeCaption);
                GUI.color = previous;
            }

            float height = _theme.Code.CalcHeight(new GUIContent(code), EditorGUIUtility.currentViewWidth) + 4f;

            EditorGUILayout.SelectableLabel(code, _theme.Code, GUILayout.Height(height));
        }

        /// <summary>
        /// What one card needs: its title row, its summary, and the signature under a hairline
        /// when it carries one.
        /// </summary>
        private float CardHeight(HelpPart part, float width)
        {
            float inner = width - _theme.CardPadding * 2f;
            float height = _theme.CardPadding * 2f;

            height += TitleRowHeight(part, inner);
            height += 6f;
            height += _theme.PartSummary.CalcHeight(new GUIContent(part.Summary), inner);

            if (!string.IsNullOrEmpty(part.Signature))
            {
                height += 10f;
                height += 1f;
                height += 6f;
                height += _theme.PartSignature.CalcHeight(new GUIContent(part.Signature), inner);
            }

            return height;
        }

        /// <summary>
        /// The title, or the icon beside it, whichever is taller. A card with no icon gives the
        /// title the whole row.
        /// </summary>
        private float TitleRowHeight(HelpPart part, float inner)
        {
            float titleWidth = string.IsNullOrEmpty(part.Icon)
                ? inner
                : inner - _theme.CardIconSize - 6f;

            float height = _theme.PartTitle.CalcHeight(new GUIContent(part.Title), titleWidth);

            return string.IsNullOrEmpty(part.Icon) ? height : Mathf.Max(height, _theme.CardIconSize);
        }

        private void DrawPart(Rect rect, HelpPart part)
        {
            EditorGUI.DrawRect(rect, _theme.CardFill);

            Handles.BeginGUI();
            Handles.color = _theme.CardBorder;
            Handles.DrawAAPolyLine(1.5f,
                new Vector3(rect.xMin, rect.yMin), new Vector3(rect.xMax, rect.yMin),
                new Vector3(rect.xMax, rect.yMax), new Vector3(rect.xMin, rect.yMax),
                new Vector3(rect.xMin, rect.yMin));
            Handles.EndGUI();

            float padding = _theme.CardPadding;
            float inner = rect.width - padding * 2f;
            float x = rect.x + padding;
            float y = rect.y + padding;

            float titleRow = TitleRowHeight(part, inner);
            float titleX = x;
            float titleWidth = inner;

            if (!string.IsNullOrEmpty(part.Icon))
            {
                GUIContent icon = EditorGUIUtility.IconContent(part.Icon);

                if (icon != null && icon.image != null)
                {
                    GUI.DrawTexture(
                        new Rect(x, y + (titleRow - _theme.CardIconSize) * 0.5f,
                            _theme.CardIconSize, _theme.CardIconSize),
                        icon.image, ScaleMode.ScaleToFit);
                }

                titleX += _theme.CardIconSize + 6f;
                titleWidth -= _theme.CardIconSize + 6f;
            }

            GUI.Label(new Rect(titleX, y, titleWidth, titleRow), part.Title, _theme.PartTitle);

            y += titleRow + 6f;

            float summaryHeight = _theme.PartSummary.CalcHeight(new GUIContent(part.Summary), inner);
            GUI.Label(new Rect(x, y, inner, summaryHeight), part.Summary, _theme.PartSummary);

            if (string.IsNullOrEmpty(part.Signature))
                return;

            y += summaryHeight + 10f;

            EditorGUI.DrawRect(new Rect(x, y, inner, 1f), _theme.CardBorder);

            y += 1f + 6f;

            float signatureHeight = _theme.PartSignature.CalcHeight(new GUIContent(part.Signature), inner);

            Color previous = GUI.color;
            GUI.color = _theme.MutedText;
            GUI.Label(new Rect(x, y, inner, signatureHeight), part.Signature, _theme.PartSignature);
            GUI.color = previous;
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

        /// <summary>
        /// A diagram drawn as a map rather than as a walk: every box at once, no Previous and
        /// Next, and no rule or code underneath. A page owns one stepped diagram, so this is how
        /// a second picture - a command sequence beside the flow that runs it - gets drawn.
        /// </summary>
        internal void Graph(HelpGraph graph) => Graph(graph, _mapStepper);

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