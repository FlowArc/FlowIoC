#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.Help.Graph
{
    /// <summary>
    /// Turns a HelpGraph into boxes and arrows. Cells come from the graph; pixels are decided
    /// here, so a page never carries a coordinate.
    /// </summary>
    internal class HelpGraphPainter
    {
        private const float NodeWidth = 156f;
        private const float NodeHeight = 54f;
        private const float ColumnGap = 52f;
        private const float RowGap = 40f;
        private const float Padding = 10f;

        /// <summary>
        /// What an arrow is marked with, and where. An arrow is drawn before the boxes so that it
        /// disappears under them rather than crossing them, but its marking has to survive that:
        /// a word wider than the gap between two boxes would be painted over and read as cut in
        /// half. So the markings are collected here while the arrows are drawn and written on top
        /// once every box is down.
        /// </summary>
        private class EdgeLabel
        {
            public EdgeLabel(Vector3 centre, string text)
            {
                Centre = centre;
                Text = text;
            }

            public Vector3 Centre { get; }
            public string Text { get; }
        }

        private readonly HelpTheme _theme;
        private readonly List<EdgeLabel> _edgeLabels = new List<EdgeLabel>();

        private float _scale = 1f;
        private float _rowHeight;
        private float _topMargin;
        private float _styledFor;
        private GUIStyle _nodeTitle;
        private GUIStyle _nodeSubtitle;
        private GUIStyle _edgeLabel;

        public HelpGraphPainter(HelpTheme theme) => _theme = theme;

        private float NodeW => NodeWidth * _scale;
        private float NodeH => NodeHeight * _scale;

        /// <summary>
        /// The gaps grow with the boxes rather than at half their rate. The words on the arrows
        /// grow too, and a gap that fell behind them left every marking wider than the space it
        /// had - which is how a word ends up written across the box beside it.
        /// </summary>
        private float ColumnSpace => ColumnGap * _scale;

        private float RowSpace => RowGap * _scale;

        public void Draw(HelpGraph graph, HelpGraphStepper stepper)
        {
            _scale = graph.Scale;
            BuildStyles();

            int columns = 1;
            int rows = 1;

            foreach (HelpGraphNode node in graph.Nodes)
            {
                if (node.Column + 1 > columns) columns = node.Column + 1;
                if (node.Row + 1 > rows) rows = node.Row + 1;
            }

            _rowHeight = TallestNode(graph);

            // An arrow that skips a column is bowed over the top of the boxes, so the top row
            // needs room above it for that curve to live in.
            _topMargin = SkipsAColumn(graph) ? _rowHeight * 0.85f : 0f;

            float width = Padding * 2f + columns * NodeW + (columns - 1) * ColumnSpace;
            float height = Padding * 2f + _topMargin + rows * _rowHeight + (rows - 1) * RowSpace;

            Rect canvas = GUILayoutUtility.GetRect(width, height, GUILayout.ExpandWidth(true));

            string activeNodeId = stepper.Count == 0 ? null : graph.Steps[stepper.Index].NodeId;

            if (Event.current.type == EventType.Repaint)
            {
                _edgeLabels.Clear();

                foreach (HelpGraphEdge edge in graph.Edges)
                    DrawEdge(canvas, graph, edge, activeNodeId);

                foreach (HelpGraphNode node in graph.Nodes)
                    DrawNode(canvas, node, node.Id == activeNodeId);

                foreach (EdgeLabel label in _edgeLabels)
                    DrawEdgeLabel(label);
            }

            DrawControls(stepper);
        }

        private void DrawControls(HelpGraphStepper stepper)
        {
            if (stepper.Count == 0)
                return;

            EditorGUILayout.Space(2f);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!stepper.CanGoPrevious))
                {
                    if (GUILayout.Button("◀ Previous", GUILayout.Width(92f)))
                        stepper.Previous();
                }

                using (new EditorGUI.DisabledScope(!stepper.CanGoNext))
                {
                    if (GUILayout.Button("Next ▶", GUILayout.Width(92f)))
                        stepper.Next();
                }

                GUILayout.Label($"Step {stepper.Index + 1} of {stepper.Count}", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
            }
        }

        /// <summary>
        /// The label styles for the current scale, rebuilt only when the scale changes: a page
        /// keeps one scale, so this runs once rather than every repaint.
        /// </summary>
        private void BuildStyles()
        {
            if (_nodeTitle != null && Mathf.Approximately(_styledFor, _scale))
                return;

            // The window measures each label and gives it a rectangle of exactly that size, so
            // both styles centre inside what they are handed rather than against the box.
            _nodeTitle = new GUIStyle(_theme.NodeTitle)
            {
                fontSize = Mathf.RoundToInt(_theme.NodeTitle.fontSize * _scale),
                alignment = TextAnchor.MiddleCenter
            };

            _nodeSubtitle = new GUIStyle(_theme.NodeSubtitle)
            {
                fontSize = Mathf.RoundToInt((_theme.NodeSubtitle.fontSize <= 0
                    ? 10
                    : _theme.NodeSubtitle.fontSize) * _scale),
                alignment = TextAnchor.MiddleCenter
            };

            _edgeLabel = new GUIStyle(_theme.EdgeLabel)
            {
                fontSize = Mathf.RoundToInt((_theme.EdgeLabel.fontSize <= 0
                    ? 10
                    : _theme.EdgeLabel.fontSize) * _scale)
            };

            _styledFor = _scale;
        }

        /// <summary>
        /// Every box in one diagram is the same height, and that height is whatever the wordiest
        /// box needs. A long title like HeroConnectorSubContext wraps onto a second line, and a
        /// fixed height would cut it off.
        /// </summary>
        private float TallestNode(HelpGraph graph)
        {
            float tallest = NodeH;

            foreach (HelpGraphNode node in graph.Nodes)
            {
                float needed = ContentHeight(node) + Inset * 2f;

                if (needed > tallest)
                    tallest = needed;
            }

            return tallest;
        }

        /// <summary>
        /// The title style sized so the title fits the box. A type name carries no spaces, so a
        /// name a shade too long for one line is broken after its second-to-last letter - which
        /// reads as a mistake. Dropping a point or two of type is quieter than that.
        /// </summary>
        private GUIStyle FittedTitle(HelpGraphNode node)
        {
            float innerWidth = NodeW - Inset * 2f;
            int size = Mathf.RoundToInt(_theme.NodeTitle.fontSize * _scale);
            int floor = Mathf.Max(8, Mathf.RoundToInt(size * 0.78f));

            _nodeTitle.fontSize = size;

            while (_nodeTitle.fontSize > floor
                   && _nodeTitle.CalcSize(new GUIContent(node.Title)).x > innerWidth)
            {
                _nodeTitle.fontSize--;
            }

            return _nodeTitle;
        }

        private float ContentHeight(HelpGraphNode node)
        {
            float innerWidth = NodeW - Inset * 2f;
            float height = FittedTitle(node).CalcHeight(new GUIContent(node.Title), innerWidth);

            if (!string.IsNullOrEmpty(node.Subtitle))
            {
                height += 2f * _scale;
                height += _nodeSubtitle.CalcHeight(new GUIContent(node.Subtitle), innerWidth);
            }

            return height;
        }

        private bool SkipsAColumn(HelpGraph graph)
        {
            foreach (HelpGraphEdge edge in graph.Edges)
            {
                HelpGraphNode from = graph.Node(edge.FromId);
                HelpGraphNode to = graph.Node(edge.ToId);

                if (from != null && to != null && from.Row == to.Row
                    && Mathf.Abs(to.Column - from.Column) > 1)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The point halfway along the curve rather than halfway along the straight line between
        /// its ends. A bowed arrow's label and its cross belong on the curve.
        /// </summary>
        private Vector3 Midpoint(Vector3 start, Vector3 startTangent, Vector3 endTangent, Vector3 end) =>
            (start + 3f * startTangent + 3f * endTangent + end) / 8f;

        private float Inset => 6f * _scale;

        private Rect NodeRect(Rect canvas, HelpGraphNode node)
        {
            float x = canvas.x + Padding + node.Column * (NodeW + ColumnSpace);
            float y = canvas.y + Padding + _topMargin + node.Row * (_rowHeight + RowSpace);
            return new Rect(x, y, NodeW, _rowHeight);
        }

        private void DrawNode(Rect canvas, HelpGraphNode node, bool active)
        {
            Rect rect = NodeRect(canvas, node);

            EditorGUI.DrawRect(rect, active ? _theme.NodeFillActive : _theme.NodeFill);

            Handles.BeginGUI();
            Handles.color = active ? _theme.NodeBorderActive : _theme.NodeBorder;
            Handles.DrawAAPolyLine(active ? 3f : 1.5f,
                new Vector3(rect.xMin, rect.yMin), new Vector3(rect.xMax, rect.yMin),
                new Vector3(rect.xMax, rect.yMax), new Vector3(rect.xMin, rect.yMax),
                new Vector3(rect.xMin, rect.yMin));
            Handles.EndGUI();

            float innerWidth = rect.width - Inset * 2f;
            float contentHeight = ContentHeight(node);
            GUIStyle title = FittedTitle(node);
            float titleHeight = title.CalcHeight(new GUIContent(node.Title), innerWidth);
            float top = rect.y + (rect.height - contentHeight) * 0.5f;

            GUI.Label(new Rect(rect.x + Inset, top, innerWidth, titleHeight), node.Title, title);

            if (string.IsNullOrEmpty(node.Subtitle))
                return;

            float subtitleTop = top + titleHeight + 2f * _scale;
            float subtitleHeight = _nodeSubtitle.CalcHeight(new GUIContent(node.Subtitle), innerWidth);

            GUI.Label(new Rect(rect.x + Inset, subtitleTop, innerWidth, subtitleHeight),
                node.Subtitle, _nodeSubtitle);
        }

        private void DrawEdge(Rect canvas, HelpGraph graph, HelpGraphEdge edge, string activeNodeId)
        {
            HelpGraphNode from = graph.Node(edge.FromId);
            HelpGraphNode to = graph.Node(edge.ToId);

            if (from == null || to == null)
                return;

            Rect fromRect = NodeRect(canvas, from);
            Rect toRect = NodeRect(canvas, to);

            Vector3 start;
            Vector3 end;
            Vector3 startTangent;
            Vector3 endTangent;

            int columnsApart = Mathf.Abs(to.Column - from.Column);

            if (from.Row == to.Row && columnsApart > 1)
            {
                // An arrow that skips a column would otherwise be drawn straight through the box
                // between the two, and its cross would be hidden behind that box. Bow it over the
                // top instead, where the whole line and its marking stay visible.
                float lift = _rowHeight * 0.75f;

                start = new Vector3(fromRect.center.x, fromRect.yMin);
                end = new Vector3(toRect.center.x, toRect.yMin);
                startTangent = start + new Vector3(0f, -lift);
                endTangent = end + new Vector3(0f, -lift);
            }
            else if (from.Row == to.Row)
            {
                bool forward = to.Column > from.Column;
                start = new Vector3(forward ? fromRect.xMax : fromRect.xMin, fromRect.center.y);
                end = new Vector3(forward ? toRect.xMin : toRect.xMax, toRect.center.y);
                float bend = forward ? 24f : -24f;
                startTangent = start + new Vector3(bend, 0f);
                endTangent = end - new Vector3(bend, 0f);
            }
            else
            {
                bool downward = to.Row > from.Row;
                start = new Vector3(fromRect.center.x, downward ? fromRect.yMax : fromRect.yMin);
                end = new Vector3(toRect.center.x, downward ? toRect.yMin : toRect.yMax);
                float bend = downward ? 22f : -22f;
                startTangent = start + new Vector3(0f, bend);
                endTangent = end - new Vector3(0f, bend);
            }

            Color color = edge.Kind == HelpGraphEdgeKind.Forbidden
                ? _theme.ArrowForbidden
                : from.Id == activeNodeId || to.Id == activeNodeId
                    ? _theme.ArrowActive
                    : _theme.Arrow;

            Handles.BeginGUI();
            Handles.DrawBezier(start, end, startTangent, endTangent, color, null, 2.5f);
            DrawArrowHead(end, (end - endTangent).normalized, color);

            if (edge.Kind == HelpGraphEdgeKind.Forbidden)
                DrawCross(Midpoint(start, startTangent, endTangent, end), color);

            Handles.EndGUI();

            if (string.IsNullOrEmpty(edge.Label))
                return;

            _edgeLabels.Add(new EdgeLabel(Midpoint(start, startTangent, endTangent, end), edge.Label));
        }

        /// <summary>
        /// The marking on an arrow, written after every box is down. It carries a strip of the
        /// page's own background behind it, because a word that reaches over a box would otherwise
        /// be two words - one on the page and one on the box - in two different shades.
        /// </summary>
        private void DrawEdgeLabel(EdgeLabel label)
        {
            GUIContent content = new GUIContent(label.Text);
            Vector2 size = _edgeLabel.CalcSize(content);
            float width = size.x + 4f;
            float height = size.y;

            Rect rect = new Rect(label.Centre.x - width * 0.5f, label.Centre.y - height - 5f,
                width, height);

            EditorGUI.DrawRect(rect, _theme.PageFill);
            GUI.Label(rect, content, _edgeLabel);
        }

        private void DrawArrowHead(Vector3 tip, Vector3 direction, Color color)
        {
            if (direction == Vector3.zero)
                direction = Vector3.right;

            Vector3 side = new Vector3(-direction.y, direction.x);

            Handles.color = color;
            Handles.DrawAAConvexPolygon(
                tip,
                tip - direction * 9f + side * 4.5f,
                tip - direction * 9f - side * 4.5f);
        }

        private void DrawCross(Vector3 centre, Color color)
        {
            Handles.color = color;
            Handles.DrawAAPolyLine(3f, centre + new Vector3(-6f, -6f), centre + new Vector3(6f, 6f));
            Handles.DrawAAPolyLine(3f, centre + new Vector3(-6f, 6f), centre + new Vector3(6f, -6f));
        }
    }
}

#endif