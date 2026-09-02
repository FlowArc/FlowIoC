#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using FlowIoC.BaseModule.Root;
using FlowIoC.Editor.Root;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
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
        [MenuItem("Tools/FlowIoC/Screens", false, -1148)]
        internal static void Open()
        {
            ScreenPanelWindow window = GetWindow<ScreenPanelWindow>("Screens");
            window.minSize = new Vector2(640, 320);
            window.Show();
        }

        private readonly Color _collisionColor = new Color(1f, 0.45f, 0.45f);

        private ScreenSubContextDeclarations _declarations;
        private ScreenPanelScan _scan;
        private ScreenLayerCollisions _collisions;
        private ScreenOverrideSeed _seed;
        private RootDirtyMarker _dirtyMarker;

        private List<ScreenRowEVO> _rows = new List<ScreenRowEVO>();
        private HashSet<ScreenRowEVO> _collided = new HashSet<ScreenRowEVO>();
        private bool _manyScenes;
        private Vector2 _scroll;

        private void OnEnable()
        {
            _declarations = new ScreenSubContextDeclarations();
            _scan = new ScreenPanelScan(_declarations);
            _collisions = new ScreenLayerCollisions();
            _seed = new ScreenOverrideSeed();
            _dirtyMarker = new RootDirtyMarker();

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
            RootBase[] roots = Object.FindObjectsByType<RootBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            _rows = _scan.Rows(roots);
            _collided = _collisions.Find(_rows);
            _manyScenes = SceneManager.sceneCount > 1;

            Repaint();
        }

        private void OnGUI()
        {
            DrawToolbar();

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

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label($"{_rows.Count} screen(s)", EditorStyles.miniLabel);

            if (_collided.Count > 0)
            {
                Color previous = GUI.color;
                GUI.color = _collisionColor;
                GUILayout.Label($"{_collided.Count} sharing a layer", EditorStyles.miniBoldLabel);
                GUI.color = previous;
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                _declarations = new ScreenSubContextDeclarations();
                _scan = new ScreenPanelScan(_declarations);
                Rescan();
            }

            EditorGUILayout.EndHorizontal();
        }

        private int ManagerOf(ScreenRowEVO row) => row.Effective?.ManagerId ?? 0;

        private void DrawManagerGroup(int managerId)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"Manager {managerId}", EditorStyles.boldLabel);

            DrawHeaderRow();

            List<ScreenRowEVO> group = _rows
                .Where(row => ManagerOf(row) == managerId)
                .OrderBy(row => row.Effective?.Layer ?? int.MaxValue)
                .ThenBy(row => row.ContextName)
                .ToList();

            foreach (ScreenRowEVO row in group)
                DrawRow(row);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);
        }

        private void DrawHeaderRow()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Screen", EditorStyles.miniBoldLabel, GUILayout.Width(180));
            GUILayout.Label("Root", EditorStyles.miniBoldLabel, GUILayout.Width(160));
            GUILayout.Label("Mgr", EditorStyles.miniBoldLabel, GUILayout.Width(40));
            GUILayout.Label("Layer", EditorStyles.miniBoldLabel, GUILayout.Width(50));
            GUILayout.Label("Tag", EditorStyles.miniBoldLabel, GUILayout.Width(80));
            GUILayout.Label("Show", EditorStyles.miniBoldLabel, GUILayout.Width(40));
            GUILayout.Label("Hide", EditorStyles.miniBoldLabel, GUILayout.Width(40));
            GUILayout.Label("Load", EditorStyles.miniBoldLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawRow(ScreenRowEVO row)
        {
            EditorGUILayout.BeginHorizontal();

            bool collided = _collided.Contains(row);
            Color previous = GUI.color;
            if (collided)
                GUI.color = _collisionColor;

            string name = row.IsOverridden ? row.ContextName + " *" : row.ContextName;
            GUIContent nameContent = new GUIContent(
                name,
                row.IsOverridden ? "Overridden on " + row.Root.name : row.ContextFullName);

            if (GUILayout.Button(nameContent, EditorStyles.label, GUILayout.Width(180)))
                EditorGUIUtility.PingObject(row.Root);

            string rootLabel = _manyScenes ? $"{row.Root.name}  ({row.SceneName})" : row.Root.name;
            GUILayout.Label(rootLabel, GUILayout.Width(160));

            GUI.color = previous;

            if (row.Effective == null)
            {
                EditorGUILayout.LabelField(new GUIContent("declaration unreadable", row.DeclarationError));
                EditorGUILayout.EndHorizontal();
                return;
            }

            DrawEditableCells(row, collided);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawEditableCells(ScreenRowEVO row, bool collided)
        {
            EditorGUI.BeginChangeCheck();

            int managerId = EditorGUILayout.IntField(row.Effective.ManagerId, GUILayout.Width(40));

            Color previous = GUI.color;
            if (collided)
                GUI.color = _collisionColor;

            int layer = EditorGUILayout.IntField(row.Effective.Layer, GUILayout.Width(50));

            GUI.color = previous;

            ScreenTag tag = (ScreenTag) EditorGUILayout.EnumPopup(row.Effective.Tag, GUILayout.Width(80));
            bool show = EditorGUILayout.Toggle(row.Effective.HasShowAnimation, GUILayout.Width(40));
            bool hide = EditorGUILayout.Toggle(row.Effective.HasHideAnimation, GUILayout.Width(40));

            if (EditorGUI.EndChangeCheck())
                Write(row, managerId, layer, tag, show, hide);

            GUILayout.Label(
                new GUIContent(
                    row.Declaration == null ? "-" : $"{row.Declaration.Load.Kind}: {row.Declaration.Load.Key}",
                    collided ? "Another screen opens on this layer of this manager. Opening one closes the other." : ""),
                EditorStyles.miniLabel);

            using (new EditorGUI.DisabledScope(!row.IsOverridden))
            {
                if (GUILayout.Button(
                        new GUIContent("Reset", "Drop this Root's override and take what the context declares."),
                        EditorStyles.miniButton,
                        GUILayout.Width(52)))
                    ResetToCode(row);
            }
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
