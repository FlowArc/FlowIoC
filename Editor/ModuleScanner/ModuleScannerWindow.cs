#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using FlowIoC.Editor.Inspector;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.ModuleScanner
{
    /// <summary>
    /// Every module's health in one list, and one button that repairs what can be repaired
    /// safely.
    ///
    /// It replaces the Assembly Creator window and the two Module Configuration menu items, which
    /// between them had a dependency order nothing stated - the namespace settings skip a module
    /// whose index entry is stale or whose assembly is missing - and no way to say what was
    /// actually wrong.
    ///
    /// The window only draws. Scanning is ModuleScannerRunner's job and repairing is ModuleRepair's,
    /// the same division ScreenScannerWindow makes with ScreenScannerRunner.
    /// </summary>
    internal class ModuleScannerWindow : EditorWindow
    {
        /// <summary>What the panel is called, in the menu, on the tab and on its bar.</summary>
        private const string TITLE = "Module Scanner";

        private const string ONLY_ISSUES_KEY = "FlowIoC.ModuleScanner.OnlyIssues";
        private const string SUMMARY_KEY = "FlowIoC.ModuleScanner.Summary";

        private const float ARROW_WIDTH = 16f;
        private const float ICON_WIDTH = 16f;
        private const float NAME_WIDTH = 220f;
        private const float BADGE_WIDTH = 78f;
        private const float FINDING_INDENT = 26f;

        [MenuItem("Tools/FlowIoC/" + TITLE, false, -1250)]
        internal static void Open()
        {
            ModuleScannerWindow window = GetWindow<ModuleScannerWindow>(TITLE);
            window.minSize = new Vector2(640, 360);
            window.Show();
        }

        /// <summary>
        /// Fix All while there is something to fix. A vivid green, because the toolbar tints a
        /// button rather than filling it and anything softer disappears into the strip.
        /// </summary>
        private readonly Color _actionColor = new Color(0.35f, 0.95f, 0.45f);

        private readonly FlowRowPainter _painter = new FlowRowPainter();

        private readonly Dictionary<string, bool> _expanded = new Dictionary<string, bool>();

        private ModuleScannerReportEVO _report;
        private ProjectTargetEVO _project;
        private List<ModuleTargetEVO> _modules;
        private FlowHeaderBar _bar;
        private bool _onlyIssues = true;
        private bool _projectExpanded = true;
        private string _summary;
        private Vector2 _scroll;
        private GUIStyle _action;


        private void OnEnable()
        {
            // The tab is named here rather than only at GetWindow, so a window restored from a
            // saved layout under the panel's old name renames itself instead of keeping it.
            titleContent = new GUIContent(TITLE);

            // Without this the window is sent no MouseMove events at all, and a row would only
            // light up when something else happened to repaint it.
            wantsMouseMove = true;

            _onlyIssues = EditorPrefs.GetBool(ONLY_ISSUES_KEY, true);
            _bar = new FlowHeaderBar(new FlowPalette(), new FlowHelpPageMap());

            // A repair that wrote an asmdef triggered a domain reload and took this window with
            // it, so the summary comes back from SessionState rather than from a field.
            _summary = SessionState.GetString(SUMMARY_KEY, string.Empty);
            SessionState.EraseString(SUMMARY_KEY);

            Rescan();
        }

        private void OnFocus() => Rescan();

        private void Rescan()
        {
            (ProjectTargetEVO project, List<ModuleTargetEVO> modules) = new ModuleTargetFactory().Build();

            _project = project;
            _modules = modules;
            _report = new ModuleScannerRunner(new ModuleCheckPipeline()).Run(project, modules);

            Repaint();
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.MouseMove) Repaint();

            // The bar wears the same green the settled rows do rather than a role's colour: no
            // FlowRole is about a module's health, and this window is about nothing else.
            _bar.DrawWindow(
                _painter.Bar, _painter.Ok, TITLE, "FlowIoC", "Every module in the project", "Refresh", Rescan,
                TITLE);

            DrawToolbar();

            if (!string.IsNullOrEmpty(_summary))
                EditorGUILayout.HelpBox(_summary, MessageType.Info);

            DrawList();
            DrawFixAll();
        }

        private void DrawList()
        {
            if (_report == null)
            {
                GUILayout.FlexibleSpace();

                return;
            }

            if (_report.Modules.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No modules found. A module is a folder whose name ends in \"Module\", under "
                    + "Assets/Modules or inside an embedded package.",
                    MessageType.Info);
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawProjectRow();

            int drawn = 0;

            foreach (ModuleRowEVO row in _report.Modules)
            {
                if (_onlyIssues && row.Status == ModuleCheckStatus.Ok) continue;

                DrawModuleRow(row);
                drawn++;
            }

            if (drawn == 0 && _report.Modules.Count > 0)
                DrawNote(ModuleCheckStatus.Ok, "Every module is in order.");

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            int modules = _report?.Modules.Count ?? 0;
            int issues = _report?.IssueCount ?? 0;
            int affected = _report?.ModulesWithIssues ?? 0;

            GUILayout.Label(Count(modules, "module"), EditorStyles.miniLabel, GUILayout.Width(70));

            Color previous = GUI.color;

            // The worst status rather than a flat red: a scan whose only issue is one Fix All
            // clears should not read as loudly as one waiting on a person.
            GUI.color = ColorFor(WorstStatus());

            GUILayout.Label(IssueSummary(issues, affected), EditorStyles.miniLabel);

            GUI.color = previous;

            GUILayout.FlexibleSpace();

            bool onlyIssues = GUILayout.Toggle(
                _onlyIssues, "Only issues", EditorStyles.toolbarButton, GUILayout.Width(80));

            if (onlyIssues != _onlyIssues)
            {
                _onlyIssues = onlyIssues;
                EditorPrefs.SetBool(ONLY_ISSUES_KEY, onlyIssues);
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// The window's one action, along the foot of it: the same shape a Root's Add Sub Context
        /// has, because it is the same kind of thing - what this panel is for, rather than one
        /// control among several in a toolbar.
        /// </summary>
        private void DrawFixAll()
        {
            // Fix All writes asmdefs, which starts a compile. Stacking that on a compile already
            // running, or on play mode, is how a half-written assembly happens.
            bool busy = EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode;
            bool fixable = !busy && (_report?.IssueCount ?? 0) > 0;

            using (new EditorGUI.DisabledScope(!fixable))
            {
                // Tinted only while it can be pressed. A disabled control is drawn washed out, and
                // a washed out green reads as a button that did something rather than as one
                // waiting.
                Color background = GUI.backgroundColor;

                if (fixable) GUI.backgroundColor = _actionColor;

                if (GUILayout.Button("Fix All", ActionStyle()))
                    FixAll();

                GUI.backgroundColor = background;
            }
        }

        /// <summary>
        /// Tall enough to read as the panel's action, and inset a little on every side - the rows
        /// above run edge to edge, and a button that did the same would not read as a button.
        /// </summary>
        private GUIStyle ActionStyle()
        {
            return _action ??= new GUIStyle(GUI.skin.button)
            {
                fixedHeight = 36f,
                margin = new RectOffset(6, 6, 4, 6)
            };
        }

        private void FixAll()
        {
            RepairResultEVO result = new ModuleRepair(new ModuleCheckPipeline())
                .Apply(_report, _project, _modules);

            var summary = new StringBuilder(result.Summary);

            foreach (string remaining in result.Remaining)
                summary.Append('\n').Append(remaining);

            _summary = summary.ToString();
            SessionState.SetString(SUMMARY_KEY, _summary);

            AssetDatabase.Refresh();
            Rescan();
        }

        private void DrawProjectRow()
        {
            ModuleCheckStatus status = WorstProject();

            _projectExpanded = DrawHeaderRow(_projectExpanded, status, "Project", null, "PROJECT");

            if (_projectExpanded && _report != null)
            {
                foreach (FindingEVO finding in _report.Project)
                    DrawFinding(finding);
            }

            GUILayout.Space(2f);
        }

        /// <summary>The worst status anywhere in the scan: what the whole report amounts to.</summary>
        private ModuleCheckStatus WorstStatus()
        {
            ModuleCheckStatus worst = WorstProject();

            if (_report == null) return worst;

            foreach (ModuleRowEVO row in _report.Modules)
            {
                if (row.Status > worst) worst = row.Status;
            }

            return worst;
        }

        private ModuleCheckStatus WorstProject()
        {
            ModuleCheckStatus worst = ModuleCheckStatus.Ok;

            if (_report == null) return worst;

            foreach (FindingEVO finding in _report.Project)
            {
                if (finding.Status > worst) worst = finding.Status;
            }

            return worst;
        }

        private void DrawModuleRow(ModuleRowEVO row)
        {
            bool expanded = _expanded.TryGetValue(row.Name, out bool value) && value;

            _expanded[row.Name] = DrawHeaderRow(
                expanded, row.Status, row.Name, row.AssemblyName, row.Kind.ToString().ToUpperInvariant());

            if (_expanded[row.Name])
            {
                foreach (FindingEVO finding in row.Findings)
                    DrawFinding(finding);
            }

            GUILayout.Space(2f);
        }

        /// <summary>
        /// One row of the list: the status as a stripe and an icon, the name, what the row is made
        /// of, and what kind of thing it is. The whole row is the foldout, so the reader does not
        /// have to find a triangle to open it.
        /// </summary>
        private bool DrawHeaderRow(bool expanded, ModuleCheckStatus status, string label, string subtitle,
            string badge)
        {
            Rect rect = _painter.Row();
            Color accent = ColorFor(status);

            _painter.Paint(rect, accent, Alpha(status));

            bool hovered = _painter.IsHovered(rect);
            float x = rect.x + _painter.ContentX - 1f;

            GUI.Label(new Rect(x, rect.y, ARROW_WIDTH, rect.height), expanded ? "▾" : "▸", _painter.Arrow);
            x += ARROW_WIDTH;

            Color previous = GUI.color;
            GUI.color = accent;
            GUI.Label(new Rect(x, rect.y, ICON_WIDTH, rect.height), IconFor(status), _painter.Icon);
            GUI.color = previous;
            x += ICON_WIDTH + 4f;

            GUI.Label(new Rect(x, rect.y, NAME_WIDTH, rect.height), label, _painter.Name(hovered));
            x += NAME_WIDTH + 6f;

            float room = rect.xMax - BADGE_WIDTH - 10f - x;

            if (!string.IsNullOrEmpty(subtitle) && room > 40f)
                GUI.Label(new Rect(x, rect.y, room, rect.height), subtitle, _painter.Mini(hovered));

            if (!string.IsNullOrEmpty(badge))
                GUI.Label(new Rect(rect.xMax - BADGE_WIDTH - 6f, rect.y, BADGE_WIDTH, rect.height), badge,
                    _painter.Badge(hovered));

            // Drawn last and painting nothing, so it takes the click without covering the row.
            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                expanded = !expanded;

            return expanded;
        }

        private void DrawFinding(FindingEVO finding)
        {
            if (_onlyIssues && finding.Status == ModuleCheckStatus.Ok) return;

            Color accent = ColorFor(finding.Status);
            Color previous = GUI.color;

            EditorGUILayout.BeginHorizontal();

            GUILayout.Space(FINDING_INDENT);

            GUI.color = accent;
            GUILayout.Label(IconFor(finding.Status), _painter.Icon, GUILayout.Width(ICON_WIDTH));
            GUI.color = previous;

            GUILayout.Label(finding.Message, EditorStyles.wordWrappedMiniLabel);
            GUILayout.FlexibleSpace();

            if (finding.Status != ModuleCheckStatus.Ok)
            {
                GUI.color = accent;
                GUILayout.Label(finding.Status.ToString(), EditorStyles.miniLabel, GUILayout.Width(55));
                GUI.color = previous;
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// What the list has to say when it has no rows to say it with. It wears the same tint a
        /// row of that status would, so "everything is in order" is green rather than the grey of
        /// a help box.
        /// </summary>
        private void DrawNote(ModuleCheckStatus status, string message)
        {
            Rect rect = _painter.Row();
            Color accent = ColorFor(status);

            _painter.Paint(rect, accent, Alpha(status));

            float x = rect.x + _painter.ContentX - 1f + ARROW_WIDTH;

            Color previous = GUI.color;
            GUI.color = accent;
            GUI.Label(new Rect(x, rect.y, ICON_WIDTH, rect.height), IconFor(status), _painter.Icon);
            GUI.color = previous;

            x += ICON_WIDTH + 4f;

            GUI.Label(new Rect(x, rect.y, rect.width - x - 6f, rect.height), message,
                _painter.Mini(_painter.IsHovered(rect)));
        }

        /// <summary>
        /// How hard a row is tinted. A settled row is fainter than a row with something to say, so
        /// a project of forty green modules does not shout as loudly as the one red row in it.
        /// </summary>
        private float Alpha(ModuleCheckStatus status)
        {
            return status == ModuleCheckStatus.Ok ? FlowRowPainter.QUIET_ALPHA : FlowRowPainter.FILL_ALPHA;
        }

        /// <summary>
        /// What the toolbar says about the scan. A count with no issues says so in a word, because
        /// "0 issues" is the answer a reader has to stop and parse.
        /// </summary>
        private string IssueSummary(int issues, int affected)
        {
            if (issues == 0) return "no issues";

            return affected == 0
                ? Count(issues, "issue")
                : $"{Count(issues, "issue")} in {Count(affected, "module")}";
        }

        private string Count(int value, string noun) => value == 1 ? $"1 {noun}" : $"{value} {noun}s";

        /// <summary>
        /// Fixable is amber and not red: it is a job waiting rather than a decision anyone has to
        /// make. Manual is red, because Fix All will not clear it however many times it is pressed.
        /// </summary>
        private Color ColorFor(ModuleCheckStatus status)
        {
            switch (status)
            {
                case ModuleCheckStatus.Fixable: return _painter.Warn;
                case ModuleCheckStatus.Manual: return _painter.Error;
                default: return _painter.Ok;
            }
        }

        private string IconFor(ModuleCheckStatus status)
        {
            switch (status)
            {
                case ModuleCheckStatus.Fixable: return "⚠";
                case ModuleCheckStatus.Manual: return "✖";
                default: return "✔";
            }
        }
    }
}

#endif