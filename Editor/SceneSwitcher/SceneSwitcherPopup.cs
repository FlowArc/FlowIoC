#if UNITY_EDITOR && UNITY_6000_3_OR_NEWER

using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.Editor.Inspector;
using FlowIoC.Editor.Modules;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace FlowIoC.Editor.SceneSwitcher
{
    /// <summary>
    /// The list the scene switcher drops down. A search on top, the tabs under it - the scenes
    /// this developer opens most, every scene by module, the screens' test scenes, the modules'
    /// test scenes - and the game's own scenes pinned above whichever tab is open, because they
    /// belong to no tab and everybody looks for them. A search looks through every scene whatever
    /// the tab says, so a scene is never missed for being on another one.
    ///
    /// Colour says one thing only: which role the module's Root has. A heading and every row wear
    /// the colour the Root's header bar wears in the inspector - Service, System, Core - and a row
    /// brightens under the pointer.
    /// </summary>
    public class SceneSwitcherPopup : PopupWindowContent
    {
        private const float WIDTH = 380f;
        private const float MAX_HEIGHT = 560f;
        private const float SEARCH_HEIGHT = 24f;
        private const float TABS_HEIGHT = 22f;
        private const float ICON_SIZE = 16f;
        private const float ARROW_WIDTH = 14f;
        private const float BADGE_WIDTH = 56f;
        private const float ACTIVE_ALPHA = 0.4f;
        private const int FREQUENT_COUNT = 8;

        private readonly string[] _tabLabels = { "Frequent", "Modules", "Screens", "Tests" };

        private readonly IReadOnlyList<SceneEntry> _scenes;
        private readonly SceneSwitcherUsage _usage;
        private readonly string _activePath;
        private readonly Action<string> _open;

        private readonly FlowPalette _palette = new FlowPalette();
        private readonly FlowRowPainter _rows = new FlowRowPainter();
        private readonly ModuleRootRole _roles = new ModuleRootRole();
        private readonly Dictionary<string, Color> _roleColors = new Dictionary<string, Color>();
        private readonly Dictionary<SceneKind, Texture> _icons = new Dictionary<SceneKind, Texture>();
        private readonly List<SceneEntry> _pinned = new List<SceneEntry>();
        private readonly List<Row> _visible = new List<Row>();

        private SearchField _searchField;
        private string _search = string.Empty;
        private SceneSwitcherTab _tab;
        private Vector2 _scroll;
        private string _emptyMessage;
        private readonly float _height;

        public SceneSwitcherPopup(IReadOnlyList<SceneEntry> scenes, SceneSwitcherUsage usage, string activePath,
            Action<string> open)
        {
            _scenes = scenes;
            _usage = usage;
            _activePath = activePath;
            _open = open;

            foreach (SceneEntry scene in _scenes)
            {
                if (scene.Kind == SceneKind.Game) _pinned.Add(scene);
            }

            _pinned.Sort((left, right) => string.CompareOrdinal(left.Name, right.Name));

            // Sized once, off every scene under its module's heading - the longest the list gets,
            // every group open - so the window does not jump between tabs, while groups open, or
            // while a search narrows the list.
            AddByModule(scene => scene.Kind != SceneKind.Game);
            _height = Mathf.Min(MAX_HEIGHT, ListTop() + _visible.Count * FlowRowPainter.ROW_HEIGHT + 4f);

            _tab = _usage.Tab;
            Rebuild();
        }

        public override Vector2 GetWindowSize() => new Vector2(WIDTH, _height);

        public override void OnOpen()
        {
            editorWindow.wantsMouseMove = true;

            _searchField = new SearchField();
            _searchField.SetFocus();

            _icons[SceneKind.Game] = EditorGUIUtility.IconContent("SceneAsset Icon").image;
            _icons[SceneKind.Check] = EditorGUIUtility.IconContent("UnityEditor.DeviceSimulation.SimulatorWindow").image;
            _icons[SceneKind.Test] = EditorGUIUtility.IconContent("TestNormal").image;
            _icons[SceneKind.ScreenTest] = EditorGUIUtility.IconContent("Canvas Icon").image;
        }

        public override void OnGUI(Rect rect)
        {
            HandleKeys();

            DrawSearch(new Rect(rect.x + 4f, rect.y + 4f, rect.width - 8f, SEARCH_HEIGHT - 6f));
            DrawTabs(new Rect(rect.x, rect.y + SEARCH_HEIGHT, rect.width, TABS_HEIGHT));

            float y = rect.y + SEARCH_HEIGHT + TABS_HEIGHT;

            if (ShowsPinned())
            {
                foreach (SceneEntry scene in _pinned)
                {
                    DrawScene(new Rect(rect.x, y, rect.width, FlowRowPainter.ROW_HEIGHT), scene, true);
                    y += FlowRowPainter.ROW_HEIGHT;
                }

                _rows.Darken(new Rect(rect.x, y, rect.width, 1f), 0.5f);
                y += 1f;
            }

            DrawList(new Rect(rect.x, y, rect.width, rect.yMax - y));

            if (Event.current.type == EventType.MouseMove) editorWindow.Repaint();
        }

        private void DrawSearch(Rect rect)
        {
            string search = _searchField.OnToolbarGUI(rect, _search);

            if (search == _search) return;

            _search = search;
            _scroll = Vector2.zero;
            Rebuild();
        }

        private void DrawTabs(Rect rect)
        {
            // Dimmed while a search runs, because the search is looking through every tab.
            using (new EditorGUI.DisabledScope(IsSearching()))
            {
                int tab = GUI.Toolbar(rect, (int) _tab, _tabLabels,EditorStyles.toolbarButton);

                if (tab == (int) _tab) return;

                _tab = (SceneSwitcherTab) tab;
                _usage.Tab = _tab;
                _scroll = Vector2.zero;
                Rebuild();
            }
        }

        private void DrawList(Rect listRect)
        {
            float contentHeight = _visible.Count * FlowRowPainter.ROW_HEIGHT;
            float scrollbar = contentHeight > listRect.height ? 14f : 0f;
            var contentRect = new Rect(0f, 0f, listRect.width - scrollbar, contentHeight);

            _scroll = GUI.BeginScrollView(listRect, _scroll, contentRect);

            for (int i = 0; i < _visible.Count; i++)
            {
                var rowRect = new Rect(0f, i * FlowRowPainter.ROW_HEIGHT, contentRect.width, FlowRowPainter.ROW_HEIGHT);

                if (_visible[i].IsHeading) DrawHeading(rowRect, _visible[i]);
                else if (_visible[i].IsFolder) DrawFolder(rowRect, _visible[i]);
                else DrawScene(rowRect, _visible[i]);
            }

            GUI.EndScrollView();

            if (_visible.Count == 0)
                GUI.Label(new Rect(listRect.x + _rows.ContentX, listRect.y + 2f, listRect.width, FlowRowPainter.ROW_HEIGHT),
                    _emptyMessage, _rows.Muted());
        }

        private void DrawHeading(Rect rect, Row row)
        {
            _rows.Paint(rect, row.Accent, FlowRowPainter.HEADING_ALPHA);

            var labelRect = new Rect(rect.x + _rows.ContentX, rect.y, rect.width - _rows.ContentX, rect.height);
            GUI.Label(labelRect, row.Heading, _rows.Strong(false));
        }

        /// <summary>
        /// A module with more than one scene: a heading that opens and closes its scenes under it,
        /// left the way this developer last left it.
        /// </summary>
        private void DrawFolder(Rect rect, Row row)
        {
            bool hovered = _rows.IsHovered(rect);
            bool expanded = _usage.IsExpanded(row.Heading);

            _rows.Paint(rect, row.Accent, hovered ? ACTIVE_ALPHA : FlowRowPainter.HEADING_ALPHA);

            float x = rect.x + _rows.ContentX;
            GUI.Label(new Rect(x, rect.y, ARROW_WIDTH, rect.height), expanded ? "▾" : "▸", _rows.Arrow);

            x += ARROW_WIDTH + 2f;
            var name = new GUIContent(row.Heading);
            GUIStyle nameStyle = _rows.Strong(hovered);
            float nameWidth = nameStyle.CalcSize(name).x;

            GUI.Label(new Rect(x, rect.y, nameWidth, rect.height), name, nameStyle);
            GUI.Label(new Rect(x + nameWidth + 4f, rect.y, 40f, rect.height), "(" + row.Count + ")", _rows.Mini(hovered));

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                rect.Contains(Event.current.mousePosition))
            {
                Event.current.Use();
                _usage.SetExpanded(row.Heading, !expanded);
                Rebuild();

                // The list this row was drawn from has just changed under the loop drawing it.
                editorWindow.Repaint();
                GUIUtility.ExitGUI();
            }
        }

        private void DrawScene(Rect rect, Row row) => DrawScene(rect, row.Scene, row.ShowModule, row.Indented);

        private void DrawScene(Rect rect, SceneEntry scene, bool showModule, bool indented = false)
        {
            bool active = scene.Path == _activePath;
            bool hovered = _rows.IsHovered(rect);

            // Every row wears its Root's colour - Service, System, Core - and brightens under the
            // pointer; the open scene is brightest.
            float alpha = active ? ACTIVE_ALPHA : hovered ? FlowRowPainter.HEADING_ALPHA : FlowRowPainter.FILL_ALPHA;
            _rows.Paint(rect, RoleColor(scene), alpha);

            float x = rect.x + _rows.ContentX + 8f + (indented ? ARROW_WIDTH : 0f);

            if (_icons.TryGetValue(scene.Kind, out Texture icon) && icon != null)
                GUI.DrawTexture(new Rect(x, rect.y + (rect.height - ICON_SIZE) / 2f, ICON_SIZE, ICON_SIZE), icon,
                    ScaleMode.ScaleToFit);

            x += ICON_SIZE + 4f;

            float nameRight = rect.xMax - BADGE_WIDTH - 6f;
            GUIStyle nameStyle = active ? _rows.Strong(true) : _rows.Name(hovered);
            var nameContent = new GUIContent(scene.Name, scene.Path);
            float nameWidth = Mathf.Min(nameStyle.CalcSize(nameContent).x, nameRight - x);

            GUI.Label(new Rect(x, rect.y, nameWidth, rect.height), nameContent, nameStyle);

            // A row listed away from its module's heading says which module it came from.
            if (showModule && nameRight - (x + nameWidth) > 20f)
                GUI.Label(new Rect(x + nameWidth + 4f, rect.y, nameRight - x - nameWidth - 4f, rect.height),
                    scene.Module, _rows.Mini(hovered));

            // Grey, the kind already shows in the icon; only the game's own scene is picked out.
            GUIStyle badgeStyle = scene.Kind == SceneKind.Game
                ? _rows.BadgeIn(_palette.Accent(FlowRole.Core, EditorGUIUtility.isProSkin))
                : _rows.Badge(hovered);

            GUI.Label(new Rect(nameRight, rect.y, BADGE_WIDTH, rect.height), BadgeText(scene.Kind), badgeStyle);

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                rect.Contains(Event.current.mousePosition))
            {
                Event.current.Use();
                Open(scene.Path);
            }
        }

        private void HandleKeys()
        {
            Event current = Event.current;

            if (current.type != EventType.KeyDown) return;
            if (current.keyCode != KeyCode.Return && current.keyCode != KeyCode.KeypadEnter) return;

            foreach (Row row in _visible)
            {
                if (row.IsHeading || row.IsFolder) continue;

                current.Use();
                Open(row.Scene.Path);
                return;
            }

            if (ShowsPinned() && _pinned.Count > 0)
            {
                current.Use();
                Open(_pinned[0].Path);
            }
        }

        /// <summary>
        /// Closed before the scene is opened, and the scene opened a moment later: saving the
        /// current scene may ask a question, and a dialog raised from inside a closing popup's GUI
        /// loses its owner.
        /// </summary>
        private void Open(string scenePath)
        {
            editorWindow.Close();
            EditorApplication.delayCall += () => _open(scenePath);
        }

        private bool IsSearching() => !string.IsNullOrEmpty(_search);

        private bool ShowsPinned() => !IsSearching() && _pinned.Count > 0;

        private float ListTop() =>
            SEARCH_HEIGHT + TABS_HEIGHT + (_pinned.Count > 0 ? _pinned.Count * FlowRowPainter.ROW_HEIGHT + 1f : 0f);

        private void Rebuild()
        {
            _visible.Clear();

            if (IsSearching())
            {
                _emptyMessage = "No scene matches";
                AddByModule(scene => true);
                return;
            }

            switch (_tab)
            {
                case SceneSwitcherTab.Frequent:
                    _emptyMessage = "The scenes you open from here gather here";
                    AddFrequent();
                    break;
                case SceneSwitcherTab.Screen:
                    _emptyMessage = "No screen test scenes";
                    AddFlat(SceneKind.ScreenTest);
                    break;
                case SceneSwitcherTab.Test:
                    _emptyMessage = "No test scenes";
                    AddFlat(SceneKind.Test);
                    break;
                default:
                    _emptyMessage = "No scenes found";
                    AddFolders();
                    break;
            }
        }

        private void AddFrequent()
        {
            foreach (string path in _usage.MostUsed(FREQUENT_COUNT + _pinned.Count))
            {
                foreach (SceneEntry scene in _scenes)
                {
                    if (scene.Path != path || scene.Kind == SceneKind.Game) continue;
                    if (_visible.Count < FREQUENT_COUNT) _visible.Add(Row.ForScene(scene, true));
                }
            }
        }

        private void AddFlat(SceneKind kind)
        {
            var members = new List<SceneEntry>();

            foreach (SceneEntry scene in _scenes)
            {
                if (scene.Kind == kind) members.Add(scene);
            }

            members.Sort((left, right) => string.CompareOrdinal(left.Name, right.Name));

            foreach (SceneEntry scene in members)
                _visible.Add(Row.ForScene(scene, true));
        }

        /// <summary>
        /// The Modules tab. A module with one scene is that scene's row, the module named beside it:
        /// a group to open for a single row is a click for nothing. A module with more is a group
        /// that opens in place.
        /// </summary>
        private void AddFolders()
        {
            foreach (KeyValuePair<string, List<SceneEntry>> group in GroupByModule(scene => scene.Kind != SceneKind.Game))
            {
                if (group.Value.Count == 1)
                {
                    _visible.Add(Row.ForScene(group.Value[0], true));
                    continue;
                }

                _visible.Add(Row.ForFolder(group.Key, group.Value.Count, RoleColor(group.Value[0])));

                if (!_usage.IsExpanded(group.Key)) continue;

                foreach (SceneEntry scene in group.Value)
                    _visible.Add(Row.ForScene(scene, false, true));
            }
        }

        /// <summary>What a search shows: every match, open, under its module's heading.</summary>
        private void AddByModule(Func<SceneEntry, bool> belongs)
        {
            foreach (KeyValuePair<string, List<SceneEntry>> group in GroupByModule(belongs))
            {
                _visible.Add(Row.ForHeading(group.Key, RoleColor(group.Value[0])));

                foreach (SceneEntry scene in group.Value)
                    _visible.Add(Row.ForScene(scene, false));
            }
        }

        private SortedDictionary<string, List<SceneEntry>> GroupByModule(Func<SceneEntry, bool> belongs)
        {
            var groups = new SortedDictionary<string, List<SceneEntry>>(StringComparer.Ordinal);

            foreach (SceneEntry scene in _scenes)
            {
                if (!belongs(scene) || !scene.Matches(_search)) continue;

                if (!groups.TryGetValue(scene.Module, out List<SceneEntry> members))
                    groups[scene.Module] = members = new List<SceneEntry>();

                members.Add(scene);
            }

            foreach (List<SceneEntry> members in groups.Values)
                members.Sort(CompareInGroup);

            return groups;
        }

        /// <summary>Within a module the kinds keep one order - the game, a check, a test, a screen's - then by name.</summary>
        private static int CompareInGroup(SceneEntry left, SceneEntry right)
        {
            int byKind = left.Kind.CompareTo(right.Kind);

            return byKind != 0 ? byKind : string.CompareOrdinal(left.Name, right.Name);
        }

        /// <summary>
        /// The colour the module's Root wears in the inspector - Service, System, Core, Connector -
        /// read off the Root's file the way the Module Library reads it. A module whose Root says
        /// nothing is grey.
        /// </summary>
        private Color RoleColor(SceneEntry scene)
        {
            if (_roleColors.TryGetValue(scene.ModuleFolder, out Color color)) return color;

            FlowRole? role = _roles.Read(scene.ModuleFolder);
            color = _palette.Accent(role ?? FlowRole.Test, EditorGUIUtility.isProSkin);
            _roleColors[scene.ModuleFolder] = color;

            return color;
        }

        private static string BadgeText(SceneKind kind)
        {
            switch (kind)
            {
                case SceneKind.Game: return "GAME";
                case SceneKind.Check: return "CHECK";
                case SceneKind.ScreenTest: return "SCREEN";
                default: return "TEST";
            }
        }

        private readonly struct Row
        {
            public readonly bool IsHeading;
            public readonly bool IsFolder;
            public readonly string Heading;
            public readonly int Count;
            public readonly Color Accent;
            public readonly SceneEntry Scene;
            public readonly bool ShowModule;
            public readonly bool Indented;

            private Row(bool isHeading, bool isFolder, string heading, int count, Color accent, SceneEntry scene,
                bool showModule, bool indented)
            {
                IsHeading = isHeading;
                IsFolder = isFolder;
                Heading = heading;
                Count = count;
                Accent = accent;
                Scene = scene;
                ShowModule = showModule;
                Indented = indented;
            }

            public static Row ForHeading(string heading, Color accent) =>
                new Row(true, false, heading, 0, accent, default, false, false);

            public static Row ForFolder(string module, int count, Color accent) =>
                new Row(false, true, module, count, accent, default, false, false);

            public static Row ForScene(SceneEntry scene, bool showModule, bool indented = false) =>
                new Row(false, false, null, 0, default, scene, showModule, indented);
        }
    }
}

#endif
