#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using FlowIoC.Editor.Inspector;
using FlowIoC.Editor.Modules;
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
    /// The window only draws. Scanning is ModuleScannerRunner's job, repairing is ModuleRepair's
    /// and the order of the rows is ModuleTree's, the same division ScreenScannerWindow makes with
    /// ScreenScannerRunner.
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
        private const float STATUS_WIDTH = 55f;

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
        private readonly FlowRowPainter _painter = new FlowRowPainter();
        private readonly ModuleRoleBadge _badge = new ModuleRoleBadge();

        private readonly Dictionary<string, bool> _expanded = new Dictionary<string, bool>();

        private FlowTreePainter _tree;

        private ModuleScannerReportEVO _report;
        private List<ModuleTreeRowEVO<ModuleRowEVO>> _rows;
        private ProjectTargetEVO _project;
        private List<ModuleTargetEVO> _modules;
        private FlowHeaderBar _bar;
        private bool _onlyIssues = true;
        private bool _projectExpanded = true;
        private string _summary;
        private Vector2 _scroll;

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
            _tree = new FlowTreePainter(_painter);

            // A repair that wrote an asmdef triggered a domain reload and took this window with
            // it, so the summary comes back from SessionState rather than from a field.
            _summary = SessionState.GetString(SUMMARY_KEY, string.Empty);
            SessionState.EraseString(SUMMARY_KEY);

            Rescan();
        }

        private void OnFocus() => Refresh();

        /// <summary>
        /// A rescan that also drops the repair summary. The summary describes one press of Fix
        /// All, and it outlives the domain reload that press causes - but not the next time
        /// somebody comes back to the window, by which point the rows underneath it may say
        /// something else entirely. Fix All rescans without this, so its own summary survives the
        /// rescan it triggers.
        /// </summary>
        private void Refresh()
        {
            _summary = string.Empty;
            Rescan();
        }

        private void Rescan()
        {
            (ProjectTargetEVO project, List<ModuleTargetEVO> modules) = new ModuleTargetFactory().Build();

            _project = project;
            _modules = modules;
            _report = new ModuleScannerRunner(new ModuleCheckPipeline()).Run(project, modules);
            _rows = new ModuleTree().Build(_report.Modules);

            Repaint();
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.MouseMove) Repaint();

            // The bar wears the same green the settled rows do rather than a role's colour: no
            // FlowRole is about a module's health, and this window is about nothing else.
            _bar.DrawWindow(
                _painter.Bar, _painter.Ok, TITLE, "FlowIoC", "Every module in the project", "Refresh", Refresh,
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

            _tree.Begin();

            int drawn = 0;

            // A green row stays when something under it is not: the row with the issue is drawn
            // indented, and the indent has to hang from something.
            foreach (ModuleTreeRowEVO<ModuleRowEVO> entry in _rows)
            {
                if (_onlyIssues && !HasIssue(entry)) continue;

                DrawModuleRow(entry);
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

                if (fixable) GUI.backgroundColor = _painter.Action;

                if (GUILayout.Button("Fix All", _painter.ActionButton))
                    FixAll();

                GUI.backgroundColor = background;
            }
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

            _projectExpanded = DrawHeaderRow(_projectExpanded, status, "Project", null, null, 0f, out _);

            if (_projectExpanded && _report != null)
            {
                foreach (FindingEVO finding in _report.Project)
                    DrawFinding(finding, 0f);
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

        /// <summary>
        /// One module, stepped in by how deep inside other modules it sits and hung from the row
        /// of the one it lives in. The foldout opens the module's own findings and nothing else:
        /// the modules inside it are always listed, because which sits in which is what the tree
        /// is there to show, and a foldout would hide exactly that behind a click.
        /// </summary>
        private void DrawModuleRow(ModuleTreeRowEVO<ModuleRowEVO> entry)
        {
            ModuleRowEVO row = entry.Row;
            float indent = _tree.Indent(entry.Depth);

            bool expanded = _expanded.TryGetValue(row.Name, out bool value) && value;

            _expanded[row.Name] = DrawHeaderRow(
                expanded, row.Status, row.Name, row.AssemblyName, row,
                indent, out Rect rect);

            _tree.Hang(entry, rect, entry.Depth, entry.Parent);

            if (_expanded[row.Name])
            {
                foreach (FindingEVO finding in row.Findings)
                    DrawFinding(finding, indent);
            }

            GUILayout.Space(2f);
        }

        /// <summary>
        /// Whether a row, or anything under it, has more than Ok to say. "Only issues" keeps a
        /// row that is green itself when something under it is not, so that the row with the
        /// issue still has a parent to hang from.
        /// </summary>
        private bool HasIssue(ModuleTreeRowEVO<ModuleRowEVO> entry)
        {
            if (entry.Row.Status != ModuleCheckStatus.Ok) return true;

            foreach (ModuleTreeRowEVO<ModuleRowEVO> descendant in entry.Descendants)
            {
                if (descendant.Row.Status != ModuleCheckStatus.Ok) return true;
            }

            return false;
        }

        /// <summary>
        /// One row of the list: the status as a stripe and an icon, the name, what the row is made
        /// of, and what kind of thing it is. The whole row is the foldout, so the reader does not
        /// have to find a triangle to open it. The indent moves the arrow, the icon and the name
        /// and nothing else, so the assembly column and the badge line up down the whole list.
        /// </summary>
        private bool DrawHeaderRow(bool expanded, ModuleCheckStatus status, string label, string subtitle,
            ModuleRowEVO module, float indent, out Rect rect)
        {
            rect = _painter.Row();
            Color accent = ColorFor(status);

            _painter.Paint(rect, accent, Alpha(status));

            bool hovered = _painter.IsHovered(rect);
            float x = rect.x + _painter.ContentX - 1f + indent;

            GUI.Label(new Rect(x, rect.y, ARROW_WIDTH, rect.height), expanded ? "▾" : "▸", _painter.Arrow);
            x += ARROW_WIDTH;

            Color previous = GUI.color;
            GUI.color = accent;
            GUI.Label(new Rect(x, rect.y, ICON_WIDTH, rect.height), IconFor(status), _painter.Icon);
            GUI.color = previous;
            x += ICON_WIDTH + 4f;

            GUI.Label(new Rect(x, rect.y, NAME_WIDTH - indent, rect.height), label, _painter.Name(hovered));
            x += NAME_WIDTH - indent + 6f;

            float room = rect.xMax - BADGE_WIDTH - 10f - x;

            if (!string.IsNullOrEmpty(subtitle) && room > 40f)
                GUI.Label(new Rect(x, rect.y, room, rect.height), subtitle, _painter.Mini(hovered));

            Rect badge = new Rect(rect.xMax - BADGE_WIDTH - 6f, rect.y, BADGE_WIDTH, rect.height);

            if (module != null)
                GUI.Label(badge, _badge.Text(module), _badge.Style(module, _painter, hovered));
            else
                GUI.Label(badge, "PROJECT", _painter.Badge(hovered));

            // Drawn last and painting nothing, so it takes the click without covering the row.
            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                expanded = !expanded;

            return expanded;
        }

        /// <summary>
        /// One finding under a row. A finding that names an asset takes the reader to it: the row
        /// lights up under the pointer, the cursor turns into a link, and clicking pings the file
        /// in the Project window. A finding that names nothing stays quiet and takes no click,
        /// because a highlight that promises a click doing nothing is worse than no highlight.
        /// </summary>
        private void DrawFinding(FindingEVO finding, float indent)
        {
            if (_onlyIssues && finding.Status == ModuleCheckStatus.Ok) return;

            Object asset = AssetFor(finding);

            float statusWidth = finding.Status == ModuleCheckStatus.Ok ? 0f : STATUS_WIDTH;
            float textX = indent + FINDING_INDENT + ICON_WIDTH + 4f;
            float textWidth = Mathf.Max(40f, position.width - textX - statusWidth - 18f);

            float height = Mathf.Max(
                FlowRowPainter.ROW_HEIGHT,
                _painter.MiniWrapped(false).CalcHeight(new GUIContent(finding.Message), textWidth));

            Rect rect = _painter.Row(height);
            Color accent = ColorFor(finding.Status);
            bool hovered = asset != null && _painter.IsHovered(rect);

            if (hovered) _painter.Paint(rect, accent);

            Color previous = GUI.color;
            GUI.color = accent;
            GUI.Label(new Rect(rect.x + indent + FINDING_INDENT, rect.y, ICON_WIDTH, FlowRowPainter.ROW_HEIGHT),
                IconFor(finding.Status), _painter.Icon);
            GUI.color = previous;

            GUI.Label(new Rect(rect.x + textX, rect.y, textWidth, rect.height), finding.Message,
                _painter.MiniWrapped(hovered));

            if (finding.Status != ModuleCheckStatus.Ok)
            {
                GUI.color = accent;
                GUI.Label(new Rect(rect.xMax - statusWidth - 6f, rect.y, statusWidth, FlowRowPainter.ROW_HEIGHT),
                    finding.Status.ToString(), EditorStyles.miniLabel);
                GUI.color = previous;
            }

            if (asset == null) return;

            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

            // Drawn last and painting nothing, so it takes the click without covering the row.
            if (!GUI.Button(rect, GUIContent.none, GUIStyle.none)) return;

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        /// <summary>
        /// The asset a finding points at, or null when it names none or names one that is no
        /// longer there - a card deleted since the scan, say.
        /// </summary>
        private Object AssetFor(FindingEVO finding)
        {
            return string.IsNullOrEmpty(finding.AssetPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<Object>(finding.AssetPath);
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