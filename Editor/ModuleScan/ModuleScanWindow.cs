#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.Editor.Inspector;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.ModuleScan
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
    /// The window only draws. Scanning is ModuleScanRunner's job and repairing is ModuleRepair's,
    /// the same division ScreenPanelWindow makes with ScreenPanelScan.
    /// </summary>
    internal class ModuleScanWindow : EditorWindow
    {
        private const string ONLY_ISSUES_KEY = "FlowIoC.ModuleScan.OnlyIssues";
        private const string SUMMARY_KEY = "FlowIoC.ModuleScan.Summary";

        private const float ROW_HEIGHT = 20f;
        private const float STRIPE_WIDTH = 3f;
        private const float ARROW_WIDTH = 14f;
        private const float ICON_WIDTH = 16f;
        private const float NAME_WIDTH = 220f;
        private const float BADGE_WIDTH = 78f;
        private const float FINDING_INDENT = 26f;

        [MenuItem("Tools/FlowIoC/Module Scan", false, -1249)]
        internal static void Open()
        {
            ModuleScanWindow window = GetWindow<ModuleScanWindow>("Module Scan");
            window.minSize = new Vector2(640, 360);
            window.Show();
        }

        /// <summary>
        /// A module nothing is wrong with. Green is the whole report a settled row needs, which is
        /// why the row says it with its icon and its stripe rather than with a word.
        /// </summary>
        private readonly Color _okColor = new Color(0.42f, 0.78f, 0.47f);

        /// <summary>
        /// Something Fix All repairs on its own. Amber rather than red for the same reason the
        /// Screens panel uses it: it is a job waiting, not a decision anyone has to make.
        /// </summary>
        private readonly Color _fixableColor = new Color(1f, 0.8f, 0.35f);

        /// <summary>
        /// Something a person has to do. Red, because the button on this window will not clear it
        /// however many times it is pressed.
        /// </summary>
        private readonly Color _manualColor = new Color(0.94f, 0.44f, 0.4f);

        /// <summary>
        /// The bar's fill: the row green, taken down until white title text clears 4.5:1 on it.
        /// The bar and the list are then one colour rather than two greens beside each other.
        /// </summary>
        private readonly Color _barColor = new Color(0.165f, 0.431f, 0.22f);

        /// <summary>
        /// Fix All while there is something to fix. A vivid green, because the toolbar tints a
        /// button rather than filling it and anything softer disappears into the strip.
        /// </summary>
        private readonly Color _actionColor = new Color(0.35f, 0.95f, 0.45f);

        private readonly Dictionary<string, bool> _expanded = new Dictionary<string, bool>();

        private ModuleScanReportEVO _report;
        private ProjectTargetEVO _project;
        private List<ModuleTargetEVO> _modules;
        private FlowHeaderBar _bar;
        private bool _onlyIssues = true;
        private bool _projectExpanded = true;
        private string _summary;
        private Vector2 _scroll;

        private GUIStyle _arrow;
        private GUIStyle _icon;
        private GUIStyle _name;
        private GUIStyle _badge;

        private void OnEnable()
        {
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
            _report = new ModuleScanRunner(new ModuleCheckPipeline()).Run(project, modules);

            Repaint();
        }

        private void OnGUI()
        {
            // The bar wears the same green the settled rows do rather than a role's colour: no
            // FlowRole is about a module's health, and this window is about nothing else.
            _bar.DrawWindow(
                _barColor, _okColor, "Module Scan", "FlowIoC", "Every module in the project", "Refresh", Rescan,
                "Module Scan");

            DrawToolbar();

            if (!string.IsNullOrEmpty(_summary))
                EditorGUILayout.HelpBox(_summary, MessageType.Info);

            if (_report == null) return;

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

            // Fix All writes asmdefs, which starts a compile. Stacking that on a compile already
            // running, or on play mode, is how a half-written assembly happens.
            bool busy = EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode;

            bool fixable = !busy && issues > 0;

            using (new EditorGUI.DisabledScope(!fixable))
            {
                // Tinted only while it can be pressed. A disabled control is drawn washed out, and
                // a washed out green reads as a button that did something rather than as one
                // waiting.
                Color background = GUI.backgroundColor;

                if (fixable) GUI.backgroundColor = _actionColor;

                // miniButton rather than toolbarButton: a toolbar button's background is all but
                // transparent, so tinting it green barely shows.
                if (GUILayout.Button("Fix All", EditorStyles.miniButton, GUILayout.Width(60), GUILayout.Height(16f)))
                    FixAll();

                GUI.backgroundColor = background;
            }

            EditorGUILayout.EndHorizontal();
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
            Rect rect = Bleed(EditorGUILayout.GetControlRect(false, ROW_HEIGHT));
            Color accent = ColorFor(status);

            EditorGUI.DrawRect(rect, Fill(accent, status));
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, STRIPE_WIDTH, rect.height), accent);

            float x = rect.x + STRIPE_WIDTH + 5f;

            GUI.Label(new Rect(x, rect.y, ARROW_WIDTH, rect.height), expanded ? "▾" : "▸", ArrowStyle());
            x += ARROW_WIDTH;

            Color previous = GUI.color;
            GUI.color = accent;
            GUI.Label(new Rect(x, rect.y, ICON_WIDTH, rect.height), IconFor(status), IconStyle());
            GUI.color = previous;
            x += ICON_WIDTH + 4f;

            GUI.Label(new Rect(x, rect.y, NAME_WIDTH, rect.height), label, NameStyle());
            x += NAME_WIDTH + 6f;

            float room = rect.xMax - BADGE_WIDTH - 10f - x;

            if (!string.IsNullOrEmpty(subtitle) && room > 40f)
                GUI.Label(new Rect(x, rect.y, room, rect.height), subtitle, EditorStyles.miniLabel);

            if (!string.IsNullOrEmpty(badge))
                GUI.Label(new Rect(rect.xMax - BADGE_WIDTH - 6f, rect.y, BADGE_WIDTH, rect.height), badge,
                    BadgeStyle());

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
            GUILayout.Label(IconFor(finding.Status), IconStyle(), GUILayout.Width(ICON_WIDTH));
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
            Rect rect = Bleed(EditorGUILayout.GetControlRect(false, ROW_HEIGHT));
            Color accent = ColorFor(status);

            EditorGUI.DrawRect(rect, Fill(accent, status));
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, STRIPE_WIDTH, rect.height), accent);

            float x = rect.x + STRIPE_WIDTH + 5f + ARROW_WIDTH;

            Color previous = GUI.color;
            GUI.color = accent;
            GUI.Label(new Rect(x, rect.y, ICON_WIDTH, rect.height), IconFor(status), IconStyle());
            GUI.color = previous;

            x += ICON_WIDTH + 4f;

            GUI.Label(new Rect(x, rect.y, rect.width - x - 6f, rect.height), message, EditorStyles.miniLabel);
        }

        /// <summary>
        /// A row runs the full width of the window, the way the header bar does, so the list reads
        /// as one column of rows rather than as a stack of boxes floating inside a margin.
        /// </summary>
        private Rect Bleed(Rect rect) => new Rect(0f, rect.y, rect.width + rect.x, rect.height);

        /// <summary>
        /// The row's own tint: the status colour, washed out until the text on it still reads. A
        /// settled row is fainter than a row with something to say, so a list of green rows does
        /// not shout as loudly as the one red row in it.
        /// </summary>
        private Color Fill(Color accent, ModuleCheckStatus status)
        {
            float alpha = status == ModuleCheckStatus.Ok ? 0.07f : 0.13f;

            return new Color(accent.r, accent.g, accent.b, alpha);
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

        private Color ColorFor(ModuleCheckStatus status)
        {
            switch (status)
            {
                case ModuleCheckStatus.Fixable: return _fixableColor;
                case ModuleCheckStatus.Manual: return _manualColor;
                default: return _okColor;
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

        // EditorStyles is not loaded when the window's fields are, so the styles are built on use.
        private GUIStyle ArrowStyle()
        {
            return _arrow ??= new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 9
            };
        }

        private GUIStyle IconStyle()
        {
            return _icon ??= new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11
            };
        }

        private GUIStyle NameStyle()
        {
            return _name ??= new GUIStyle(EditorStyles.boldLabel) {alignment = TextAnchor.MiddleLeft};
        }

        private GUIStyle BadgeStyle()
        {
            return _badge ??= new GUIStyle(EditorStyles.miniLabel) {alignment = TextAnchor.MiddleRight};
        }
    }
}

#endif