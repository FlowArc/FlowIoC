#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Root;
using FlowIoC.Editor.Inspector;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FlowIoC.Editor.ModelViewer
{
    /// <summary>
    /// The live state of every model in the scene, in one tree: a row per Root in Initialize
    /// Order, under it the objects that module bound, under each the members it chose to show.
    /// It reads and never writes - a value changed from here would skip the rules the Model
    /// exists to keep.
    ///
    /// The tree is rebuilt from the binders on every repaint, ten times a second while playing,
    /// and the members of a row are read only while the row is open, so a closed row costs
    /// nothing and there is no depth to cap. ModelTree decides what is listed and
    /// ModelMemberReader what a value is made of; this only draws.
    /// </summary>
    internal class ModelViewerWindow : EditorWindow
    {
        /// <summary>What the panel is called, in the menu, on the tab and on its bar.</summary>
        private const string TITLE = "Model Viewer";

        private const string SHARED_PATH = "#Shared";
        private const string MORE_SEGMENT = "/…";

        private const float ARROW_WIDTH = 16f;
        private const float NAME_WIDTH = 220f;
        private const float TYPE_WIDTH = 180f;
        private const float MIN_NAME_WIDTH = 60f;

        /// <summary>Ten repaints a second: live enough to watch a value move, not one per editor frame.</summary>
        private const double REPAINT_INTERVAL = 0.1;

        private const string NOT_PLAYING = "Enter Play Mode to see live model state.";
        private const string NO_ROOT = "No Root in the scene.";

        private const string NOTHING_SHOWN =
            "nothing to show: a public member is listed unless it is [HideInModelViewer], a private one with [ShowInModelViewer]";

        [MenuItem("Tools/FlowIoC/" + TITLE, false, -1149)]
        private static void Open()
        {
            ModelViewerWindow window = GetWindow<ModelViewerWindow>(TITLE);
            window.minSize = new Vector2(560f, 360f);
            window.Show();
        }

        /// <summary>How the value cell is drawn: the value itself, a quiet subtitle, or a muted aside.</summary>
        private enum CellStyle
        {
            Text = 0,
            Mini = 1,
            Muted = 2
        }

        private readonly FlowRowPainter _painter = new FlowRowPainter();
        private readonly FlowPalette _palette = new FlowPalette();
        private readonly ModelTree _modelTree = new ModelTree();
        private readonly ModelMemberReader _reader = new ModelMemberReader();

        /// <summary>How many children a row shows past the first page. Dropped when play mode ends.</summary>
        private readonly Dictionary<string, int> _limits = new Dictionary<string, int>(StringComparer.Ordinal);

        /// <summary>The objects on the path from the Root's object to the row being drawn, for the cycle check.</summary>
        private readonly List<object> _ancestors = new List<object>();

        private readonly List<string> _ancestorNames = new List<string>();

        /// <summary>The open rows, as paths, kept through a domain reload and across play mode.</summary>
        [SerializeField] private List<string> _expandedPaths = new List<string>();

        /// <summary>Whether the Roots were opened once for a fresh window. Collapse All means what it says afterwards.</summary>
        [SerializeField] private bool _seeded;

        private FlowHeaderBar _bar;
        private FlowTreePainter _tree;
        private Vector2 _scroll;
        private double _lastRepaint;

        private void OnEnable()
        {
            // The tab is named here rather than only at GetWindow, so a window restored from a
            // saved layout under the panel's old name renames itself instead of keeping it.
            titleContent = new GUIContent(TITLE);

            // Without this the window is sent no MouseMove events at all, and a row would only
            // light up when something else happened to repaint it.
            wantsMouseMove = true;

            _bar = new FlowHeaderBar(_palette, new FlowHelpPageMap());
            _tree = new FlowTreePainter(_painter);

            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        private void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            if (EditorApplication.timeSinceStartup - _lastRepaint < REPAINT_INTERVAL) return;

            _lastRepaint = EditorApplication.timeSinceStartup;
            Repaint();
        }

        /// <summary>The note row appears and goes with play mode, without waiting for a mouse to move.</summary>
        private void OnPlayModeChanged(PlayModeStateChange change)
        {
            _limits.Clear();
            Repaint();
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.MouseMove) Repaint();

            // FlowIoC's own colour: the window is a tool, not a role.
            _bar.DrawWindow(TITLE, "FlowIoC", "Live state of every model in the scene", "Collapse All", CollapseAll, TITLE);

            if (!EditorApplication.isPlaying)
            {
                DrawNote(NOT_PLAYING);

                return;
            }

            RootBase[] found = FindObjectsByType<RootBase>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            List<ModelRootEVO> roots = _modelTree.Build(found);

            if (roots.Count == 0)
            {
                DrawNote(NO_ROOT);

                return;
            }

            Seed(roots);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            _tree.Begin();
            _ancestors.Clear();
            _ancestorNames.Clear();

            foreach (ModelRootEVO root in roots)
                DrawRoot(root);

            DrawShared(roots[0]);

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// A fresh window opens on the modules and what they bound rather than on six closed rows.
        /// Once: after Collapse All the reader's choice stands.
        /// </summary>
        private void Seed(List<ModelRootEVO> roots)
        {
            if (_seeded) return;

            _seeded = true;

            foreach (ModelRootEVO root in roots)
                Expand(RootPath(root));

            Expand(SHARED_PATH);
        }

        private void CollapseAll()
        {
            _expandedPaths.Clear();
            _seeded = true;
            Repaint();
        }

        // ---- Rows above the members

        private void DrawRoot(ModelRootEVO root)
        {
            string path = RootPath(root);
            Color accent = _palette.Accent(root.Role, EditorGUIUtility.isProSkin);
            bool foldable = root.Objects.Count > 0 || root.SubContexts.Count > 0;
            string subtitle = root.Root.Context.GetType().Name + " · " + root.Root.initializeOrder;

            if (!foldable) subtitle += " · nothing to show";

            DrawRow(path, null, 0, accent, FlowRowPainter.FILL_ALPHA, foldable, root.Root.name, subtitle, CellStyle.Mini,
                null, root.Role.ToString().ToUpperInvariant(), true, foldable ? () => Toggle(path) : (Action) null);

            if (!foldable || !IsExpanded(path)) return;

            DrawObjects(path, 1, root.Objects, accent);

            foreach (ModelContextEVO context in root.SubContexts)
            {
                string contextPath = path + "/" + context.Context.GetType().Name;

                DrawRow(contextPath, path, 1, accent, FlowRowPainter.QUIET_ALPHA, true, context.Context.GetType().Name,
                    "sub context", CellStyle.Mini, null, "SUB CONTEXT", false, () => Toggle(contextPath));

                if (IsExpanded(contextPath))
                    DrawObjects(contextPath, 2, context.Objects, accent);
            }
        }

        /// <summary>The Shared row: what the RootsManager bound before any Root, in the Adapter's colour because it is the adapters' filings.</summary>
        private void DrawShared(ModelRootEVO first)
        {
            List<ModelObjectEVO> objects = _modelTree.Shared(first.Root.Context.InjectionBinderCrossContext);

            if (objects.Count == 0) return;

            Color accent = _palette.Accent(FlowRole.Adapter, EditorGUIUtility.isProSkin);

            DrawRow(SHARED_PATH, null, 0, accent, FlowRowPainter.FILL_ALPHA, true, "Shared", "bound before any Root",
                CellStyle.Mini, null, "FRAMEWORK", true, () => Toggle(SHARED_PATH));

            if (IsExpanded(SHARED_PATH))
                DrawObjects(SHARED_PATH, 1, objects, accent);
        }

        private void DrawObjects(string parentPath, int depth, List<ModelObjectEVO> objects, Color accent)
        {
            foreach (ModelObjectEVO entry in objects)
            {
                Type type = entry.Value.GetType();
                string typeName = _reader.TypeName(type);
                string path = parentPath + "/" + typeName + "|" + (entry.Key != null ? entry.Key.Name : "") + "|" + entry.Name;
                string subtitle = entry.Key != null && entry.Key != type ? "as " + _reader.TypeName(entry.Key) : "";

                if (!string.IsNullOrEmpty(entry.Name))
                    subtitle += (subtitle.Length > 0 ? " " : "") + "[" + entry.Name + "]";

                DrawRow(path, parentPath, depth, accent, FlowRowPainter.QUIET_ALPHA, true, typeName, subtitle, CellStyle.Mini,
                    null, BadgeFor(entry.Kind), false, () => Toggle(path));

                if (IsExpanded(path))
                    DrawChildren(path, depth + 1, entry.Value, type, typeName, accent);
            }
        }

        // ---- Members

        /// <summary>
        /// What sits under an open row, read now: the first page of children, a hint when there
        /// are none, and a row offering the rest when there are more than the page holds.
        /// </summary>
        private void DrawChildren(string path, int depth, object value, Type type, string name, Color accent)
        {
            int limit = _limits.TryGetValue(path, out int raised) ? raised : ModelMemberReader.PAGE;
            List<ModelNodeEVO> children = _reader.Children(value, type, limit, out int hidden);

            _ancestors.Add(value);
            _ancestorNames.Add(name);

            // An aside rather than a row with a name: the text starts in the name column and runs on.
            if (children.Count == 0)
                DrawRow(path + "/#hint", path, depth, accent, FlowRowPainter.QUIET_ALPHA, false, "", NOTHING_SHOWN,
                    CellStyle.Muted, null, "", false, null);

            foreach (ModelNodeEVO child in children)
                DrawNode(path, depth, child, accent);

            _ancestors.RemoveAt(_ancestors.Count - 1);
            _ancestorNames.RemoveAt(_ancestorNames.Count - 1);

            if (hidden > 0)
            {
                int next = limit * 2;

                DrawRow(path + MORE_SEGMENT, path, depth, accent, FlowRowPainter.QUIET_ALPHA, false, "", "… " + hidden + " more",
                    CellStyle.Muted, null, "", false, () => _limits[path] = next);
            }
        }

        private void DrawNode(string parentPath, int depth, ModelNodeEVO node, Color accent)
        {
            string path = parentPath + "/" + node.Name;
            string typeName = _reader.TypeName(node.Type);

            if (node.Fault != null)
            {
                DrawRow(path, parentPath, depth, _painter.Error, FlowRowPainter.FILL_ALPHA, false, node.Name, node.Fault,
                    CellStyle.Mini, _painter.Error, typeName, false, null);

                return;
            }

            int ancestor = IndexOfAncestor(node.Value);

            if (ancestor >= 0)
            {
                DrawRow(path, parentPath, depth, accent, FlowRowPainter.QUIET_ALPHA, false, node.Name,
                    "↺ same object as " + _ancestorNames[ancestor], CellStyle.Muted, null, typeName, false, null);

                return;
            }

            string value = _reader.Describe(node.Value, node.Type);

            if (!_reader.IsExpandable(node.Value))
            {
                Object pinged = node.Value as Object;
                Action ping = pinged != null && pinged ? () => Ping(pinged) : (Action) null;

                DrawRow(path, parentPath, depth, accent, FlowRowPainter.QUIET_ALPHA, false, node.Name, value,
                    node.Value == null ? CellStyle.Muted : CellStyle.Text, null, typeName, false, ping);

                return;
            }

            DrawRow(path, parentPath, depth, accent, FlowRowPainter.QUIET_ALPHA, true, node.Name, value, CellStyle.Text, null,
                typeName, false, () => Toggle(path));

            if (IsExpanded(path))
                DrawChildren(path, depth + 1, node.Value, node.Type, node.Name, accent);
        }

        // ---- One row

        /// <summary>
        /// One row of the tree: the tint and stripe, the arrow when the row folds, the name, the
        /// value cell, the right-hand column, the guide line to the row it hangs from, and the
        /// click when there is one. The whole row is the control, the way Module Scanner's rows
        /// are, so the reader does not have to find a triangle. A row with no click is drawn in
        /// the idle styles and never lights up.
        /// </summary>
        private void DrawRow(string path, string parentPath, int depth, Color accent, float alpha, bool foldable,
            string name, string value, CellStyle style, Color? valueColor, string badge, bool badgeInAccent, Action onClick)
        {
            Rect rect = _painter.Row();
            _painter.Paint(rect, accent, alpha);

            bool hovered = onClick != null && _painter.IsHovered(rect);
            float x = _tree.SlotX(rect, depth);

            if (foldable)
                GUI.Label(new Rect(x, rect.y, ARROW_WIDTH, rect.height), IsExpanded(path) ? "▾" : "▸", _painter.Arrow);

            x += ARROW_WIDTH;

            float nameWidth = Mathf.Max(MIN_NAME_WIDTH, NAME_WIDTH - _tree.Indent(depth));
            bool named = !string.IsNullOrEmpty(name);

            if (named)
                GUI.Label(new Rect(x, rect.y, nameWidth, rect.height), name, _painter.Name(hovered));

            // A row with no name - the hint, the "more" row - starts its text in the name column.
            float valueX = named ? x + nameWidth + 6f : x;
            float valueWidth = rect.xMax - TYPE_WIDTH - 10f - valueX;

            if (!string.IsNullOrEmpty(value) && valueWidth > 40f)
            {
                Color previous = GUI.color;

                if (valueColor.HasValue) GUI.color = valueColor.Value;

                GUI.Label(new Rect(valueX, rect.y, valueWidth, rect.height), value, CellStyleFor(style, hovered));

                GUI.color = previous;
            }

            if (!string.IsNullOrEmpty(badge))
            {
                GUIStyle badgeStyle = badgeInAccent ? _painter.BadgeIn(accent) : _painter.Badge(hovered);
                GUI.Label(new Rect(rect.xMax - TYPE_WIDTH - 6f, rect.y, TYPE_WIDTH, rect.height), badge, badgeStyle);
            }

            _tree.Hang(path, rect, depth, parentPath);

            if (onClick == null) return;

            if (!foldable) EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

            // Drawn last and painting nothing, so it takes the click without covering the row.
            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                onClick();
        }

        private GUIStyle CellStyleFor(CellStyle style, bool hovered)
        {
            switch (style)
            {
                case CellStyle.Mini: return _painter.Mini(hovered);
                case CellStyle.Muted: return _painter.Muted();
                default: return _painter.Cell(hovered);
            }
        }

        /// <summary>
        /// What the list has to say when it has no rows to say it with: not playing, or nothing
        /// rooted. Shaded grey rather than a status colour, because nothing is wrong.
        /// </summary>
        private void DrawNote(string message)
        {
            Rect rect = _painter.Row();
            _painter.PaintShaded(rect, _painter.Guide);

            float x = rect.x + _painter.ContentX + ARROW_WIDTH;
            GUI.Label(new Rect(x, rect.y, rect.width - x - 6f, rect.height), message, _painter.Muted());
        }

        // ---- State

        private string RootPath(ModelRootEVO root) => root.Root.name;

        private bool IsExpanded(string path) => _expandedPaths.Contains(path);

        private void Expand(string path)
        {
            if (!_expandedPaths.Contains(path)) _expandedPaths.Add(path);
        }

        private void Toggle(string path)
        {
            if (!_expandedPaths.Remove(path)) _expandedPaths.Add(path);
        }

        /// <summary>
        /// Where on the current path this value already sits, or -1. Reference types only: a
        /// struct is boxed afresh on every read and is never the same object as anything.
        /// </summary>
        private int IndexOfAncestor(object value)
        {
            if (value == null || value is string || value.GetType().IsValueType) return -1;

            for (int ii = 0; ii < _ancestors.Count; ii++)
            {
                if (ReferenceEquals(_ancestors[ii], value)) return ii;
            }

            return -1;
        }

        private void Ping(Object target)
        {
            Selection.activeObject = target;
            EditorGUIUtility.PingObject(target);
        }

        private string BadgeFor(ModelKind kind)
        {
            switch (kind)
            {
                case ModelKind.Model: return "MODEL";
                case ModelKind.Service: return "SERVICE";
                case ModelKind.System: return "SYSTEM";
                case ModelKind.SubService: return "SUB SERVICE";
                case ModelKind.SubSystem: return "SUB SYSTEM";
                default: return "OBJECT";
            }
        }
    }
}

#endif
