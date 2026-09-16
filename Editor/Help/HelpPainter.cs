#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.Help.Graph;
using FlowIoC.Editor.Icons;
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
        /// <summary>How tall the bar between two topics is, and what it keeps clear either side.</summary>
        private const float SeparatorHeight = 5f;

        private const float SeparatorMargin = 10f;

        private readonly HelpTheme _theme;
        private readonly HelpGraphPainter _graphPainter;
        private readonly HelpCodeHighlighter _highlighter;
        private readonly FlowIcons _icons;

        /// <summary>
        /// What a page link is checked against, and null for a painter that is not the help
        /// window's. The generator windows build one of these to draw a code block with, and a
        /// button that opened a page would have nowhere to open it.
        /// </summary>
        private readonly HelpPageCatalog _catalog;

        /// <summary>
        /// The position a diagram drawn as a map is at: nowhere, because it has no steps. One
        /// instance answers for every such diagram, since none of them can be walked.
        /// </summary>
        private readonly HelpGraphStepper _mapStepper = new HelpGraphStepper(0);

        internal HelpPainter(HelpTheme theme) : this(theme, null, new FlowIcons())
        {
        }

        /// <summary>
        /// The help window's own painter, sharing the window's icons so a card and the sidebar
        /// row above it load one texture between them.
        /// </summary>
        internal HelpPainter(HelpTheme theme, HelpPageCatalog catalog, FlowIcons icons)
        {
            _theme = theme;
            _graphPainter = new HelpGraphPainter(theme);
            _highlighter = new HelpCodeHighlighter(theme);
            _catalog = catalog;
            _icons = icons;
        }

        /// <summary>
        /// The purple bar every page wears: its title on the left, and whatever the page can do on
        /// the right, with the module's version beside it when the page has one. The readings it
        /// offers are not here - they are a strip along the foot of the band below, where a tab
        /// sits directly on top of the page it opens.
        /// </summary>
        internal void Banner(string title, HelpAction action = null, string version = null)
        {
            Color previous = GUI.backgroundColor;
            GUI.backgroundColor = _theme.Banner;

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox,
                       GUILayout.Height(_theme.BannerHeight)))
            {
                GUILayout.Label(EditorGUIUtility.IconContent("console.infoicon"),
                    GUILayout.Width(35f), GUILayout.Height(_theme.BannerHeight));

                EditorGUILayout.LabelField(title, _theme.Heading,
                    GUILayout.Height(_theme.BannerHeight), GUILayout.ExpandWidth(false));

                GUILayout.FlexibleSpace();

                // The number sits against the button that acts on it: what is here, and beside it
                // what pressing the button does about it.
                if (!string.IsNullOrEmpty(version))
                    GUILayout.Label(version, _theme.BannerVersion, GUILayout.Height(_theme.BannerHeight));

                DrawAction(action);
            }

            GUI.backgroundColor = previous;
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

        /// <summary>
        /// A heading inside the page, on a band of the window's violet. Bold alone did not part a
        /// heading from the paragraph under it, so the heading takes the tint the sidebar gives a
        /// featured topic. The band reaches a little past the text on both sides, and the text
        /// itself stays where a paragraph starts.
        /// </summary>
        public void SubHeading(string text)
        {
            Rect row = EditorGUILayout.GetControlRect(false, _theme.PageHeadingHeight, _theme.SubHeading);

            var band = new Rect(row.x - _theme.PageHeadingInset, row.y,
                row.width + _theme.PageHeadingInset * 2f, row.height);

            EditorGUI.DrawRect(band, _theme.PageHeadingFill);
            GUI.Label(row, text, _theme.SubHeading);
        }

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
        /// A table: a header row on the band a heading wears, then a row per entry, each parted
        /// from the next by a hairline. Every column but the last is as wide as its widest cell,
        /// up to a share of the page, and the last takes what is left and wraps - so a table of
        /// names and their meanings reads down the names and across to the meaning. A row shorter
        /// than the header is padded with empty cells, and a null header draws no header row.
        ///
        /// Like a row of cards, the height has to be known before the row is reserved, so the
        /// columns are first measured against the width the view says is left after the sidebar -
        /// never wider than the truth, so never too short - and measured again across the
        /// rectangle the layout hands back before anything is drawn.
        /// </summary>
        public void Table(string[] headers, params string[][] rows)
        {
            if (rows == null || rows.Length == 0)
                return;

            int columns = headers?.Length ?? 0;

            foreach (string[] row in rows)
                columns = Mathf.Max(columns, row?.Length ?? 0);

            if (columns == 0)
                return;

            float measured = Mathf.Max(_theme.CardMinWidth,
                EditorGUIUtility.currentViewWidth - _theme.ContentMargin);

            float height = TableHeight(headers, rows, ColumnWidths(headers, rows, columns, measured));

            GUILayout.Space(_theme.TableMargin);

            Rect table = GUILayoutUtility.GetRect(_theme.CardMinWidth, height, GUILayout.ExpandWidth(true));

            GUILayout.Space(_theme.TableMargin);

            if (Event.current.type != EventType.Repaint)
                return;

            float[] widths = ColumnWidths(headers, rows, columns, table.width);
            float y = table.y;

            if (headers != null)
            {
                float rowHeight = RowHeight(headers, widths, _theme.TableHeader);

                EditorGUI.DrawRect(new Rect(table.x, y, table.width, rowHeight), _theme.TableHeaderFill);
                DrawCells(headers, widths, table.x, y, rowHeight, _theme.TableHeader);
                EditorGUI.DrawRect(new Rect(table.x, y + rowHeight - 1f, table.width, 1f), _theme.TableRowLine);

                y += rowHeight;
            }

            foreach (string[] row in rows)
            {
                float rowHeight = RowHeight(row, widths, _theme.TableCell);

                DrawCells(row, widths, table.x, y, rowHeight, _theme.TableCell);
                EditorGUI.DrawRect(new Rect(table.x, y + rowHeight - 1f, table.width, 1f), _theme.TableRowLine);

                y += rowHeight;
            }
        }

        /// <summary>
        /// The width of every column across <paramref name="total"/>. A column other than the
        /// last is its widest cell on one line, capped at the theme's share of the table; the
        /// last is whatever those leave.
        /// </summary>
        private float[] ColumnWidths(string[] headers, string[][] rows, int columns, float total)
        {
            var widths = new float[columns];
            float cap = total * _theme.TableColumnShare;
            float used = 0f;

            for (int column = 0; column < columns - 1; column++)
            {
                float widest = CellWidth(headers, column, _theme.TableHeader);

                foreach (string[] row in rows)
                    widest = Mathf.Max(widest, CellWidth(row, column, _theme.TableCell));

                widths[column] = Mathf.Min(Mathf.Ceil(widest), cap);
                used += widths[column];
            }

            widths[columns - 1] = Mathf.Max(_theme.CardMinWidth, total - used);

            return widths;
        }

        private float CellWidth(string[] cells, int column, GUIStyle style) =>
            cells != null && column < cells.Length && !string.IsNullOrEmpty(cells[column])
                ? style.CalcSize(new GUIContent(cells[column])).x
                : 0f;

        private float TableHeight(string[] headers, string[][] rows, float[] widths)
        {
            float height = headers != null ? RowHeight(headers, widths, _theme.TableHeader) : 0f;

            foreach (string[] row in rows)
                height += RowHeight(row, widths, _theme.TableCell);

            return height;
        }

        /// <summary>
        /// The height of one row: its tallest cell once wrapped to its column, and never less
        /// than a line, so an empty row still reads as a row.
        /// </summary>
        private float RowHeight(string[] cells, float[] widths, GUIStyle style)
        {
            float height = style.CalcHeight(new GUIContent(" "), widths[0]);

            for (int column = 0; column < widths.Length; column++)
            {
                if (cells == null || column >= cells.Length || string.IsNullOrEmpty(cells[column]))
                    continue;

                height = Mathf.Max(height, style.CalcHeight(new GUIContent(cells[column]), widths[column]));
            }

            return Mathf.Ceil(height) + 1f;
        }

        private void DrawCells(string[] cells, float[] widths, float x, float y, float height, GUIStyle style)
        {
            for (int column = 0; column < widths.Length; column++)
            {
                if (cells != null && column < cells.Length && !string.IsNullOrEmpty(cells[column]))
                    GUI.Label(new Rect(x, y, widths[column], height - 1f), cells[column], style);

                x += widths[column];
            }
        }

        /// <summary>
        /// A button that opens another page of this window. A topic explained in full elsewhere is
        /// pointed at rather than repeated, and the reader arrives on the page rather than being
        /// told which entry of the sidebar to go and find.
        ///
        /// A title the catalogue does not carry draws nothing. A page is free to link to something
        /// only a private package installs, and a project without that package sees the paragraph
        /// without the button rather than a button that goes nowhere.
        /// </summary>
        public void PageLink(string pageTitle, string label = null)
        {
            if (_catalog == null || _catalog.FindPage(pageTitle) == null)
                return;

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(6f);

                if (GUILayout.Button(label ?? pageTitle, _theme.PageLink, GUILayout.ExpandWidth(false)))
                    HelpWindow.OpenPage(pageTitle);

                GUILayout.FlexibleSpace();
            }
        }

        /// <summary>
        /// The bar a page parts two topics with. Five pixels rather than one, because it ends a
        /// block rather than parting two rows: a dark edge, a body of the same dark, and a light
        /// line under it that reads as the page starting again.
        ///
        /// It is the page's mark to make. Nothing draws it for a page, so a heading that follows
        /// on from what came before is left to follow on, and the bar means a change of subject.
        /// </summary>
        public void Separator()
        {
            GUILayout.Space(SeparatorMargin);

            Rect row = GUILayoutUtility.GetRect(0f, SeparatorHeight, GUILayout.ExpandWidth(true));

            GUILayout.Space(SeparatorMargin);

            if (Event.current.type != EventType.Repaint)
                return;

            // The margins the page is written inside are taken back off, so the bar runs the whole
            // width of the page. A rule that stops where the text stops reads as part of the
            // paragraph above it; one that reaches both edges is what says a topic has ended.
            Rect bar = new Rect(row.x - _theme.PageBodyPadding, row.y,
                row.width + _theme.PageBodyPadding * 2f, row.height);

            EditorGUI.DrawRect(bar, _theme.PageSeparatorFill);
            EditorGUI.DrawRect(new Rect(bar.x, bar.y, bar.width, 1f), _theme.PageSeparatorEdge);
            EditorGUI.DrawRect(new Rect(bar.x, bar.yMax - 1f, bar.width, 1f), _theme.PageSeparatorLight);
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

            CodeCaption(code, caption);

            var content = new GUIContent(_highlighter.Highlight(code));
            float height = _theme.Code.CalcHeight(content, EditorGUIUtility.currentViewWidth) + 4f;

            Rect rect = GUILayoutUtility.GetRect(content, _theme.Code,
                GUILayout.Height(height), GUILayout.ExpandWidth(true));

            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, _theme.CodeFill);

                Handles.BeginGUI();
                Handles.color = _theme.CodeBorder;
                Handles.DrawAAPolyLine(1.5f,
                    new Vector3(rect.xMin, rect.yMin), new Vector3(rect.xMax, rect.yMin),
                    new Vector3(rect.xMax, rect.yMax), new Vector3(rect.xMin, rect.yMax),
                    new Vector3(rect.xMin, rect.yMin));
                Handles.EndGUI();
            }

            GUI.Label(rect, content, _theme.Code);
        }

        /// <summary>
        /// The row above a code block: which file the snippet is from on the left, and the button
        /// that puts it on the clipboard on the right. The button is here rather than inside the
        /// block because a snippet is one long line as often as not, and a control floating over
        /// the code would sit on top of it.
        /// </summary>
        private void CodeCaption(string code, string caption)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (!string.IsNullOrEmpty(caption))
                {
                    Color previous = GUI.color;
                    GUI.color = _theme.MutedText;
                    GUILayout.Label(caption, _theme.CodeCaption, GUILayout.ExpandWidth(false));
                    GUI.color = previous;
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Copy", _theme.CodeCopy, GUILayout.Width(_theme.CodeCopyWidth)))
                    EditorGUIUtility.systemCopyBuffer = code;
            }
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
            float titleWidth = part.Icon == FlowIcon.None
                ? inner
                : inner - _theme.CardIconSize - 6f;

            float height = _theme.PartTitle.CalcHeight(new GUIContent(part.Title), titleWidth);

            return part.Icon == FlowIcon.None ? height : Mathf.Max(height, _theme.CardIconSize);
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

            if (part.Icon != FlowIcon.None)
            {
                _icons.Draw(
                    new Rect(Mathf.Round(x), Mathf.Round(y + (titleRow - _theme.CardIconSize) * 0.5f),
                        _theme.CardIconSize, _theme.CardIconSize),
                    part.Icon, _theme.PartTitle.normal.textColor);

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
        /// It sits in a dark well a few pixels wider than itself, sunk into the page, so the
        /// picture stands out from the grey around it. A picture the project no longer ships
        /// draws nothing rather than a magenta rectangle: the page is still worth reading
        /// without it.
        /// </summary>
        public void Image(Texture2D image, string caption = null)
        {
            if (image == null)
                return;

            float padding = _theme.ImageWellPadding;
            float available = EditorGUIUtility.currentViewWidth - _theme.ImageMargin - padding * 2f;
            float width = Mathf.Min(image.width, available);
            float height = width * image.height / image.width;

            Rect well = GUILayoutUtility.GetRect(width + padding * 2f, height + padding * 2f,
                GUILayout.ExpandWidth(false));

            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(well, _theme.ImageWellFill);

                // Sunk, not raised: the shadow along the top and the left, the light along the
                // bottom and the right.
                EditorGUI.DrawRect(new Rect(well.x, well.y, well.width, 1f), _theme.ImageWellShadow);
                EditorGUI.DrawRect(new Rect(well.x, well.y, 1f, well.height), _theme.ImageWellShadow);
                EditorGUI.DrawRect(new Rect(well.x, well.yMax - 1f, well.width, 1f), _theme.ImageWellLight);
                EditorGUI.DrawRect(new Rect(well.xMax - 1f, well.y, 1f, well.height), _theme.ImageWellLight);

                GUI.DrawTexture(new Rect(well.x + padding, well.y + padding, width, height), image,
                    ScaleMode.ScaleToFit);
            }

            if (!string.IsNullOrEmpty(caption))
                EditorGUILayout.LabelField(caption, _theme.Caption, GUILayout.Width(well.width));

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