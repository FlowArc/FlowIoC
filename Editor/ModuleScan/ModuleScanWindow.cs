#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
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

        [MenuItem("Tools/FlowIoC/Module Scan", false, -1249)]
        internal static void Open()
        {
            ModuleScanWindow window = GetWindow<ModuleScanWindow>("Module Scan");
            window.minSize = new Vector2(640, 360);
            window.Show();
        }

        private readonly Dictionary<string, bool> _expanded = new Dictionary<string, bool>();

        private ModuleScanReportEVO _report;
        private ProjectTargetEVO _project;
        private List<ModuleTargetEVO> _modules;
        private bool _onlyIssues = true;
        private bool _projectExpanded = true;
        private string _summary;
        private Vector2 _scroll;

        private void OnEnable()
        {
            _onlyIssues = EditorPrefs.GetBool(ONLY_ISSUES_KEY, true);

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
                EditorGUILayout.HelpBox("Every module is in order.", MessageType.Info);

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label(
                $"{_report?.Modules.Count ?? 0} modules · {_report?.IssueCount ?? 0} issues",
                EditorStyles.miniLabel);

            GUILayout.FlexibleSpace();

            bool onlyIssues = GUILayout.Toggle(
                _onlyIssues, "Only issues", EditorStyles.toolbarButton, GUILayout.Width(80));

            if (onlyIssues != _onlyIssues)
            {
                _onlyIssues = onlyIssues;
                EditorPrefs.SetBool(ONLY_ISSUES_KEY, onlyIssues);
            }

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
                Rescan();

            // Fix All writes asmdefs, which starts a compile. Stacking that on a compile already
            // running, or on play mode, is how a half-written assembly happens.
            bool busy = EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode;

            using (new EditorGUI.DisabledScope(busy))
            {
                if (GUILayout.Button("Fix All", EditorStyles.toolbarButton, GUILayout.Width(60)))
                    FixAll();
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
            EditorGUILayout.BeginVertical("box");

            _projectExpanded = EditorGUILayout.Foldout(
                _projectExpanded, IconFor(WorstProject()) + "  Project", true);

            if (_projectExpanded && _report != null)
            {
                foreach (FindingEVO finding in _report.Project)
                    DrawFinding(finding);
            }

            EditorGUILayout.EndVertical();
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
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();

            bool expanded = _expanded.TryGetValue(row.Name, out bool value) && value;
            bool next = EditorGUILayout.Foldout(expanded, IconFor(row.Status) + "  " + row.Name, true);
            _expanded[row.Name] = next;

            GUILayout.FlexibleSpace();
            GUILayout.Label(row.Kind.ToString(), EditorStyles.miniLabel, GUILayout.Width(60));

            EditorGUILayout.EndHorizontal();

            if (next)
            {
                GUILayout.Label("    " + row.AssemblyName, EditorStyles.miniLabel);

                foreach (FindingEVO finding in row.Findings)
                    DrawFinding(finding);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawFinding(FindingEVO finding)
        {
            if (_onlyIssues && finding.Status == ModuleCheckStatus.Ok) return;

            EditorGUILayout.BeginHorizontal();

            GUILayout.Space(16);
            GUILayout.Label(
                IconFor(finding.Status) + "  " + finding.Message, EditorStyles.wordWrappedMiniLabel);

            GUILayout.FlexibleSpace();

            if (finding.Status != ModuleCheckStatus.Ok)
                GUILayout.Label(finding.Status.ToString(), EditorStyles.miniLabel, GUILayout.Width(55));

            EditorGUILayout.EndHorizontal();
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
