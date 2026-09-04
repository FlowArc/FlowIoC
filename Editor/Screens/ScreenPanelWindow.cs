#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using FlowIoC.BaseModule.Root;
using FlowIoC.Editor.Inspector;
using FlowIoC.Editor.Root;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.ViewsMediators.Manager;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace FlowIoC.Editor.Screens
{
    /// <summary>
    /// Every screen context on a Root in the open scenes, grouped by the ScreenManager it registers
    /// at. A screen's layer is a question about the scene it opens in, which is why this window is
    /// about the open scenes and not about the project.
    ///
    /// Editing a cell writes the Root entry's override - the same field the Root inspector writes -
    /// because the declaration itself lives in the context's code. Load is not editable here for
    /// the same reason it is not there.
    /// </summary>
    internal class ScreenPanelWindow : EditorWindow
    {
        /// <summary>What the panel is called, in the menu, on the tab and on its bar.</summary>
        private const string TITLE = "Screens";

        /// <summary>The strip of column names over a group. Shorter than a row: it holds no field.</summary>
        private const float HEADING_HEIGHT = 16f;

        [MenuItem("Tools/FlowIoC/" + TITLE, false, -1148)]
        internal static void Open()
        {
            ScreenPanelWindow window = GetWindow<ScreenPanelWindow>(TITLE);

            // Wide enough for every column at once. The columns are fixed widths, so a narrower
            // window would not shrink them - it would push Load and Reset off the right edge.
            window.minSize = new Vector2(720, 320);
            window.Show();
        }

        private readonly FlowRowPainter _painter = new FlowRowPainter();

        private ScreenSubContextDeclarations _declarations;
        private ScreenPanelScan _scan;
        private ScreenLayerCollisions _collisions;
        private ScreenOverrideSeed _seed;
        private RootDirtyMarker _dirtyMarker;
        private FlowHeaderBar _bar;

        private List<ScreenRowEVO> _rows = new List<ScreenRowEVO>();
        private HashSet<ScreenRowEVO> _collided = new HashSet<ScreenRowEVO>();

        /// <summary>
        /// The ScreenManager in the open scenes each id names, so a group heading can select the
        /// object it is about. An id with no manager is not an error here - the screens are
        /// registered either way, and the manager's own inspector is what reports duplicates.
        /// </summary>
        private Dictionary<int, ScreenManager> _managers = new Dictionary<int, ScreenManager>();

        private bool _manyScenes;
        private Vector2 _scroll;

        private void OnEnable()
        {
            // The tab is named here rather than only at GetWindow, so a window restored from a
            // saved layout - or opened by anything but the menu item - never shows the type name.
            titleContent = new GUIContent(TITLE);

            // Without this the window is sent no MouseMove events at all, and a row would only
            // light up when something else happened to repaint it.
            wantsMouseMove = true;

            _declarations = new ScreenSubContextDeclarations();
            _scan = new ScreenPanelScan(_declarations);
            _collisions = new ScreenLayerCollisions();
            _seed = new ScreenOverrideSeed();
            _dirtyMarker = new RootDirtyMarker();
            _bar = new FlowHeaderBar(new FlowPalette(), new FlowHelpPageMap());

            EditorApplication.hierarchyChanged += Rescan;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneClosed += OnSceneClosed;

            Rescan();
        }

        private void OnDisable()
        {
            EditorApplication.hierarchyChanged -= Rescan;
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneClosed -= OnSceneClosed;
        }

        private void OnFocus() => Rescan();

        private void OnSceneOpened(Scene scene, OpenSceneMode mode) => Rescan();

        private void OnSceneClosed(Scene scene) => Rescan();

        // A scan instantiates context types to read their declarations, so it runs on a change
        // rather than on every repaint.
        private void Rescan()
        {
            // A docked window is focused before it is enabled after a domain reload, so OnFocus
            // can arrive while the scan itself is still null.
            if (_scan == null) return;

            RootBase[] roots = Object.FindObjectsByType<RootBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            _rows = _scan.Rows(roots);
            _collided = _collisions.Find(_rows);
            _managers = FindManagers();
            _manyScenes = SceneManager.sceneCount > 1;

            Repaint();
        }

        /// <summary>
        /// One manager per id. Two managers on one id is a scene that will not behave, and the
        /// ScreenManager inspector already says so in as many words; here the first one found is
        /// what the heading selects, because a heading that refuses to select anything would be
        /// the least helpful answer to a scene in that state.
        /// </summary>
        private Dictionary<int, ScreenManager> FindManagers()
        {
            var managers = new Dictionary<int, ScreenManager>();

            foreach (ScreenManager manager in Object.FindObjectsByType<ScreenManager>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                int id = manager.ManagerData?.ManagerID ?? 0;

                if (!managers.ContainsKey(id)) managers.Add(id, manager);
            }

            return managers;
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.MouseMove) Repaint();

            DropFocusOnClick();

            // The bar wears the green its settled rows do rather than the Screen role's gold, the
            // way Module Scanner does: what this window reports is a state, not a role.
            _bar.DrawWindow(
                _painter.Bar, _painter.Ok, "Screens", "FlowIoC", "Screens in the open scenes", "Refresh", Refresh,
                "Screens");

            if (_rows.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No screen contexts in the open scenes. A screen is listed here once its module's Root is in the scene "
                    + "with the screen's context added as a sub context.",
                    MessageType.Info);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            foreach (int managerId in _rows.Select(ManagerOf).Distinct().OrderBy(id => id).ToList())
                DrawManagerGroup(managerId);

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// Takes the keyboard focus off whatever holds it whenever the reader clicks, before the
        /// controls draw - so the field actually under the pointer claims it back during the same
        /// event, and a click on anything else leaves nothing focused.
        ///
        /// This is what makes the manager and layer cells commit. They are delayed fields, which
        /// write on Return or on losing focus, and an EditorWindow does not drop focus on a click
        /// into empty space the way the inspector does: without this, typing a layer and clicking
        /// elsewhere in the panel left the number on screen and nothing written.
        /// </summary>
        private void DropFocusOnClick()
        {
            if (Event.current.type == EventType.MouseDown) GUIUtility.keyboardControl = 0;
        }

        /// <summary>
        /// A rescan reads the declarations the context types already loaded hold. Refresh throws
        /// those away first, so a declaration edited in code since the window opened is read again.
        /// </summary>
        private void Refresh()
        {
            _declarations = new ScreenSubContextDeclarations();
            _scan = new ScreenPanelScan(_declarations);

            Rescan();
        }

        private int ManagerOf(ScreenRowEVO row) => row.Effective?.ManagerId ?? 0;

        private void DrawManagerGroup(int managerId)
        {
            List<ScreenRowEVO> group = _rows
                .Where(row => ManagerOf(row) == managerId)
                .OrderBy(row => row.Effective?.Layer ?? int.MaxValue)
                .ThenBy(row => row.ContextName)
                .ToList();

            DrawGroupHeader(managerId, group);

            foreach (ScreenRowEVO row in group)
                DrawRow(row);

            GUILayout.Space(6f);
        }

        /// <summary>
        /// The group's two lines - which manager it registers at, and what the columns under it
        /// are - painted as one rect. Two rows would leave the layout's own gap between them, and
        /// a grey line across a green header is the seam this avoids.
        /// </summary>
        private void DrawGroupHeader(int managerId, List<ScreenRowEVO> group)
        {
            Rect block = _painter.Row(FlowRowPainter.ROW_HEIGHT + HEADING_HEIGHT);
            _painter.Paint(block, _painter.Ok, FlowRowPainter.HEADING_ALPHA);

            DrawManagerLine(
                managerId, group, new Rect(block.x, block.y, block.width, FlowRowPainter.ROW_HEIGHT));

            DrawColumnHeadings(
                new Rect(block.x, block.y + FlowRowPainter.ROW_HEIGHT, block.width, HEADING_HEIGHT));
        }

        /// <summary>
        /// Which manager this group registers at, and how many screens it holds. It stays green
        /// whatever the rows under it say: the heading is where the group starts, and a colour
        /// that moves would compete with the rows that carry the actual answer.
        /// </summary>
        private void DrawManagerLine(int managerId, List<ScreenRowEVO> group, Rect rect)
        {
            _managers.TryGetValue(managerId, out ScreenManager manager);

            var label = new Rect(rect.x + _painter.ContentX, rect.y, 220f, rect.height);

            // Only the name lights up, and only while there is a manager to select. A screen row
            // lights as a whole because the whole row is about one screen; here the rest of the
            // line does nothing when it is clicked, so it stays where it is.
            bool hovered = manager != null && _painter.IsHovered(label);

            var content = new GUIContent(
                $"Manager ID:{managerId}",
                manager == null
                    ? "No ScreenManager with this id is in the open scenes."
                    : $"Select {manager.name} in the hierarchy.");

            // A label when there is nothing to select, so the heading never offers a click that
            // does nothing.
            if (manager == null)
                GUI.Label(label, content, _painter.Strong(false));
            else if (GUI.Button(label, content, _painter.Strong(hovered)))
                Select(manager);

            var badge = new Rect(rect.xMax - 96f, rect.y, 90f, rect.height);
            GUI.Label(badge, group.Count == 1 ? "1 screen" : $"{group.Count} screens", _painter.Badge(false));
        }

        /// <summary>
        /// Selecting as well as pinging: a ping alone scrolls the hierarchy to the object and
        /// leaves the inspector on whatever was there before, which is not what clicking the thing
        /// the group is named after should do.
        /// </summary>
        private void Select(Component component)
        {
            Selection.activeGameObject = component.gameObject;
            EditorGUIUtility.PingObject(component.gameObject);
        }

        /// <summary>
        /// The column names, as the header bar's strip does it: the band taken down a shade under
        /// the line above and the names in the colour that line is tinted with.
        /// </summary>
        private void DrawColumnHeadings(Rect rect)
        {
            // Everything but the stripe. The stripe runs the height of the whole header in one
            // tone, so darkening over it would break the line the group hangs from.
            _painter.Darken(
                new Rect(
                    rect.x + FlowRowPainter.STRIPE_WIDTH,
                    rect.y,
                    rect.width - FlowRowPainter.STRIPE_WIDTH,
                    rect.height));

            var columns = new ScreenColumnsEVO(rect, _painter.ContentX);
            GUIStyle style = _painter.Heading(_painter.Ok);

            GUI.Label(columns.Name, "Screen", style);
            GUI.Label(columns.Root, "Root", style);
            GUI.Label(columns.Manager, "Manager", style);
            GUI.Label(columns.Layer, "Layer", style);
            GUI.Label(columns.Tag, "Tag", style);
            GUI.Label(columns.ShowAnimation, "Show Anim", style);
            GUI.Label(columns.HideAnimation, "Hide Anim", style);

            if (columns.Load.width > 0f)
                GUI.Label(columns.Load, "Load", style);
        }

        /// <summary>
        /// Green when the screen has its manager's layer to itself, amber when another screen of
        /// the same manager opens on it. Amber rather than red: the runtime allows it and a game
        /// sometimes wants it, so this is a warning and never a refusal.
        /// </summary>
        private Color LayerColor(bool collided) => collided ? _painter.Warn : _painter.Ok;

        private void DrawRow(ScreenRowEVO row)
        {
            bool collided = _collided.Contains(row);

            Rect rect = _painter.Row();
            var columns = new ScreenColumnsEVO(rect, _painter.ContentX);

            // A settled row is tinted faintly and only a collision is filled the full amount, so a
            // list of screens that are all in order does not read as a wall of green.
            _painter.Paint(
                rect,
                LayerColor(collided),
                collided ? FlowRowPainter.FILL_ALPHA : FlowRowPainter.QUIET_ALPHA);

            bool hovered = _painter.IsHovered(rect);

            string label = row.IsOverridden ? row.ContextName + " *" : row.ContextName;
            var nameContent = new GUIContent(
                label,
                row.IsOverridden ? "Overridden on " + row.Root.name : row.ContextFullName);

            if (GUI.Button(columns.Name, nameContent, _painter.Name(hovered)))
                EditorGUIUtility.PingObject(row.Root);

            // Small, the way Load is: the screen's own name is what the row is about, and where it
            // is listed and where it loads from are both answers to "and then where".
            string rootLabel = _manyScenes ? $"{row.Root.name}  ({row.SceneName})" : row.Root.name;
            GUI.Label(columns.Root, rootLabel, _painter.Mini(hovered));

            if (row.Effective == null)
            {
                GUI.Label(columns.Message, new GUIContent("declaration unreadable", row.DeclarationError),
                    _painter.Cell(hovered));

                return;
            }

            DrawEditableCells(row, columns, collided, hovered);
        }

        private void DrawEditableCells(ScreenRowEVO row, ScreenColumnsEVO columns, bool collided, bool hovered)
        {
            EditorGUI.BeginChangeCheck();

            // Delayed, because these two decide where the row sits: the manager picks its group and
            // the layer sorts it inside one. A plain IntField writes on every keystroke, so typing
            // 12 committed 1 first, moved the row into a group that did not exist a moment before,
            // and left the caret on whatever row had slid into that place.
            int managerId = EditorGUI.DelayedIntField(Field(columns.Manager), row.Effective.ManagerId);
            int layer = EditorGUI.DelayedIntField(Field(columns.Layer), row.Effective.Layer);

            var tag = (ScreenTag) EditorGUI.EnumPopup(Field(columns.Tag), row.Effective.Tag);
            bool show = AnimationToggle(columns.ShowAnimation, row.Effective.HasShowAnimation);
            bool hide = AnimationToggle(columns.HideAnimation, row.Effective.HasHideAnimation);

            if (EditorGUI.EndChangeCheck())
                Write(row, managerId, layer, tag, show, hide);

            if (columns.Load.width > 0f)
            {
                GUI.Label(
                    columns.Load,
                    new GUIContent(
                        row.Declaration == null ? "-" : $"{row.Declaration.Load.Kind}: {row.Declaration.Load.Key}",
                        collided
                            ? "Another screen opens on this layer of this manager. Opening one closes the other."
                            : ""),
                    _painter.Mini(hovered));
            }

            using (new EditorGUI.DisabledScope(!row.IsOverridden))
            {
                if (GUI.Button(
                        columns.Reset,
                        new GUIContent("Reset", "Drop this Root's override and take what the context declares."),
                        EditorStyles.miniButton))
                    ResetToCode(row);
            }
        }

        /// <summary>A field is a line tall; the row it sits in is taller, to leave the tint room.</summary>
        private Rect Field(Rect column)
        {
            return new Rect(column.x, column.y + 1f, column.width, column.height - 2f);
        }

        /// <summary>
        /// A checkbox draws at the left edge of whatever rect it is given, which would leave it
        /// under the first letter of a two word heading. This centres it in its column instead.
        /// </summary>
        private bool AnimationToggle(Rect column, bool value)
        {
            const float box = 14f;
            var rect = new Rect(column.x + (column.width - box) * 0.5f, column.y + 3f, box, box);

            return EditorGUI.Toggle(rect, value);
        }

        private void Write(ScreenRowEVO row, int managerId, int layer, ScreenTag tag, bool show, bool hide)
        {
            if (!TryTakeEntry(row, out SubContextData entry))
                return;

            if (!entry.OverrideScreen)
            {
                entry.OverrideScreen = true;
                entry = _seed.Apply(entry, row.Declaration);
            }

            entry.ScreenManagerId = managerId;
            entry.ScreenLayer = layer;
            entry.ScreenTag = tag;
            entry.ScreenHasShowAnimation = show;
            entry.ScreenHasHideAnimation = hide;

            Commit(row, entry, "screen-panel-edit");
        }

        private void ResetToCode(ScreenRowEVO row)
        {
            if (!TryTakeEntry(row, out SubContextData entry))
                return;

            entry.OverrideScreen = false;

            Commit(row, entry, "screen-panel-reset");
        }

        /// <summary>
        /// The entry the row was scanned from, if it is still there. Someone may have removed a sub
        /// context in the inspector since the scan, which would leave the index pointing at another
        /// context or past the end.
        /// </summary>
        private bool TryTakeEntry(ScreenRowEVO row, out SubContextData entry)
        {
            entry = default;

            if (row.Root == null || row.Root.SubContextTypes == null || row.EntryIndex >= row.Root.SubContextTypes.Count)
            {
                Rescan();
                return false;
            }

            entry = row.Root.SubContextTypes[row.EntryIndex];

            if (entry.ContextFullName != row.ContextFullName)
            {
                Rescan();
                return false;
            }

            return true;
        }

        private void Commit(ScreenRowEVO row, SubContextData entry, string undoName)
        {
            Undo.RecordObject(row.Root, undoName);
            row.Root.SubContextTypes[row.EntryIndex] = entry;
            _dirtyMarker.Mark(row.Root);

            Rescan();
        }
    }
}
#endif