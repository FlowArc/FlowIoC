#if UNITY_EDITOR

using System.IO;
using System.Linq;
using FlowIoC.Editor.AgentRules;
using FlowIoC.Editor.AgentSkills;
using FlowIoC.Editor.Inspector;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.AgentScanner
{
    /// <summary>
    /// What this project tells its AI assistants, in one list: the rule block in AGENTS.md and
    /// CLAUDE.md, and the skill folders under .claude/skills. Both are files FlowIoC owns and
    /// keeps current, so the question a reader has about either one is the same - is it there,
    /// and does it describe the version the project is on.
    ///
    /// It replaces the separate Agent Rules and Agent Skills windows. They asked that one question
    /// twice, in two places, with two buttons that did the same kind of work, and neither said
    /// what the other had found.
    ///
    /// The window only draws. Writing the block is AgentRulesSynchronizer's job and writing the
    /// skills is AgentSkillsInstaller's, the same division the other two scanners make.
    /// </summary>
    internal class AgentScannerWindow : EditorWindow
    {
        /// <summary>What the panel is called, in the menu, on the tab, on its bar and in Help.</summary>
        private const string TITLE = "Agent Scanner";

        /// <summary>The strip under the group's name. Shorter than a row: it holds no field.</summary>
        private const float HEADING_HEIGHT = 16f;

        private const float ICON_WIDTH = 16f;
        private const float NAME_WIDTH = 200f;
        private const float BADGE_WIDTH = 60f;
        private const float SUMMARY_WIDTH = 160f;

        [MenuItem("Tools/FlowIoC/" + TITLE, false, -1249)]
        internal static void Open()
        {
            AgentScannerWindow window = GetWindow<AgentScannerWindow>(TITLE);
            window.minSize = new Vector2(560, 320);
            window.Show();
        }

        /// <summary>
        /// Sync while there is something to write. A vivid green, because the button is tinted
        /// rather than filled and anything softer disappears into the strip.
        /// </summary>
        private readonly Color _actionColor = new Color(0.35f, 0.95f, 0.45f);

        private readonly FlowRowPainter _painter = new FlowRowPainter();
        private readonly AgentRulesAutoSync _rulesAutoSync = new AgentRulesAutoSync();
        private readonly AgentSkillsAutoSync _skillsAutoSync = new AgentSkillsAutoSync();

        private FlowHeaderBar _bar;
        private string _projectRoot;
        private SyncFileState[] _rules;
        private SyncFileState[] _skills;
        private Vector2 _scroll;
        private GUIStyle _action;

        private void OnEnable()
        {
            // The tab is named here rather than only at GetWindow, so a window restored from a
            // saved layout under one of the two old names renames itself instead of keeping it.
            titleContent = new GUIContent(TITLE);

            _bar = new FlowHeaderBar(new FlowPalette(), new FlowHelpPageMap());
            _projectRoot = new ProjectRoot().Resolve();

            Rescan();
        }

        private void OnFocus() => Rescan();

        private void Rescan()
        {
            _rules = new AgentRulesSynchronizer(_projectRoot, new AgentRulesSource()).Inspect();
            _skills = new AgentSkillsInstaller(_projectRoot, new AgentSkillsSource()).Inspect();

            Repaint();
        }

        private void OnGUI()
        {
            // The bar wears the same green the settled rows do rather than a role's colour: no
            // FlowRole is about a file being current, and this window is about nothing else.
            _bar.DrawWindow(
                _painter.Bar, _painter.Ok, TITLE, "FlowIoC", "What this project tells its AI assistants",
                "Refresh", Rescan, TITLE);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawGroup("Agent rules", "AGENTS.md and CLAUDE.md in the project root", _rules, "RULES",
                "This version of FlowIoC ships no rule text.");

            DrawGroup("Agent skills", AgentSkillsInstaller.TargetFolder, _skills, "SKILL",
                "This version of FlowIoC ships no skills.");

            EditorGUILayout.EndScrollView();

            DrawAutoSyncToggles();
            DrawSync();
        }

        /// <summary>
        /// One section of the list: a heading naming what the rows are and where they live, and a
        /// row per file under it. Two sections rather than two windows, because the question each
        /// one answers is the same and a reader wants both answers at once.
        /// </summary>
        private void DrawGroup(string heading, string subtitle, SyncFileState[] states, string badge,
            string emptyMessage)
        {
            DrawGroupHeader(heading, subtitle, states);

            if (states == null || states.Length == 0)
            {
                DrawNote(SyncStatus.Current, emptyMessage);
            }
            else
            {
                foreach (SyncFileState state in states)
                    DrawRow(state, badge);
            }

            GUILayout.Space(6f);
        }

        /// <summary>
        /// The group's two lines - what it is, and where it lives - painted as one rect. Two rows
        /// would leave the layout's own gap between them, and a grey line across a green heading
        /// is the seam this avoids. It stays green whatever the rows say: the heading is where the
        /// group starts, and a colour that moved would compete with the rows carrying the answer.
        /// </summary>
        private void DrawGroupHeader(string heading, string subtitle, SyncFileState[] states)
        {
            Rect block = _painter.Row(FlowRowPainter.ROW_HEIGHT + HEADING_HEIGHT);
            _painter.Paint(block, _painter.Ok, FlowRowPainter.HEADING_ALPHA);

            var line = new Rect(block.x, block.y, block.width, FlowRowPainter.ROW_HEIGHT);

            GUI.Label(new Rect(line.x + _painter.ContentX, line.y, 220f, line.height), heading,
                _painter.Strong(false));

            GUI.Label(new Rect(line.xMax - SUMMARY_WIDTH - 6f, line.y, SUMMARY_WIDTH, line.height),
                Summarise(states), _painter.Badge(false));

            var strip = new Rect(block.x, block.y + FlowRowPainter.ROW_HEIGHT, block.width, HEADING_HEIGHT);
            _painter.Darken(strip);

            GUI.Label(new Rect(strip.x + _painter.ContentX, strip.y, strip.width, strip.height), subtitle,
                _painter.Heading(_painter.Ok));
        }

        /// <summary>
        /// What the group amounts to, in the words the rows use. A group with nothing to write
        /// says so in three words, because "0 out of date" is the answer a reader has to parse.
        /// </summary>
        private string Summarise(SyncFileState[] states)
        {
            if (states == null || states.Length == 0) return string.Empty;

            int pending = states.Count(NeedsWriting);
            int broken = states.Count(state => Broken(state.Status));

            if (broken > 0)
                return pending > 0 ? $"{pending} to write, {broken} broken" : Count(broken, "broken file");

            return pending == 0 ? "all up to date" : $"{Count(pending, "file")} to write";
        }

        /// <summary>
        /// One file: the status as a stripe and an icon, the file's name, what is wrong with it if
        /// anything is, and which of the two kinds it is.
        /// </summary>
        private void DrawRow(SyncFileState state, string badge)
        {
            Rect rect = _painter.Row();
            Color accent = ColorFor(state.Status);

            _painter.Paint(rect, accent, Alpha(state.Status));

            float x = rect.x + _painter.ContentX;

            Color previous = GUI.color;
            GUI.color = accent;
            GUI.Label(new Rect(x, rect.y, ICON_WIDTH, rect.height), IconFor(state.Status), _painter.Icon);
            GUI.color = previous;
            x += ICON_WIDTH + 4f;

            GUI.Label(new Rect(x, rect.y, NAME_WIDTH, rect.height), Path.GetFileName(state.Path),
                _painter.Name(false));
            x += NAME_WIDTH + 6f;

            float room = rect.xMax - BADGE_WIDTH - 10f - x;

            if (room > 40f)
                GUI.Label(new Rect(x, rect.y, room, rect.height), Describe(state), _painter.Mini(false));

            GUI.Label(new Rect(rect.xMax - BADGE_WIDTH - 6f, rect.y, BADGE_WIDTH, rect.height), badge,
                _painter.Badge(false));
        }

        /// <summary>
        /// What a section has to say when it has no rows to say it with. It wears the tint a row
        /// of that status would, so "nothing ships" is green rather than the grey of a help box.
        /// </summary>
        private void DrawNote(SyncStatus status, string message)
        {
            Rect rect = _painter.Row();
            Color accent = ColorFor(status);

            _painter.Paint(rect, accent, Alpha(status));

            float x = rect.x + _painter.ContentX;

            Color previous = GUI.color;
            GUI.color = accent;
            GUI.Label(new Rect(x, rect.y, ICON_WIDTH, rect.height), IconFor(status), _painter.Icon);
            GUI.color = previous;

            x += ICON_WIDTH + 4f;

            GUI.Label(new Rect(x, rect.y, rect.width - x - 6f, rect.height), message,
                _painter.Mini(false));
        }

        /// <summary>
        /// FlowIoC writes both whenever they are absent or stale, without asking. A project that
        /// would rather decide for itself turns one or the other off here, and then nothing of
        /// that kind is written until Sync is pressed. The two are separate on purpose: a project
        /// may want the rules in AGENTS.md and no skill folders under .claude.
        /// </summary>
        private void DrawAutoSyncToggles()
        {
            EditorGUILayout.Space();

            DrawAutoSyncToggle(_rulesAutoSync, "Keep AGENTS.md and CLAUDE.md up to date automatically");
            DrawAutoSyncToggle(_skillsAutoSync, "Keep the shipped skills up to date automatically");
        }

        private void DrawAutoSyncToggle(IAutoSyncSwitch autoSync, string label)
        {
            bool on = !autoSync.IsOff(_projectRoot);
            bool wanted = EditorGUILayout.ToggleLeft(label, on);

            if (wanted == on) return;

            if (wanted) autoSync.TurnOn(_projectRoot);
            else autoSync.TurnOff(_projectRoot);
        }

        /// <summary>
        /// The window's one action, along the foot of it: the same shape Module Scanner's Fix All
        /// has, because it is the same kind of thing - what this panel is for, rather than one
        /// control among several in a toolbar. It writes both kinds whatever the switches say,
        /// because pressing it is the asking those switches were about.
        /// </summary>
        private void DrawSync()
        {
            bool pending = Pending(_rules) || Pending(_skills);

            using (new EditorGUI.DisabledScope(!pending))
            {
                // Tinted only while it can be pressed. A disabled control is drawn washed out, and
                // a washed out green reads as a button that did something rather than one waiting.
                Color background = GUI.backgroundColor;

                if (pending) GUI.backgroundColor = _actionColor;

                if (GUILayout.Button("Sync", ActionStyle()))
                    Sync();

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

        private void Sync()
        {
            new AgentRulesSynchronizer(_projectRoot, new AgentRulesSource()).Sync();
            new AgentSkillsInstaller(_projectRoot, new AgentSkillsSource()).Install();

            AssetDatabase.Refresh();
            Rescan();
        }

        private bool Pending(SyncFileState[] states) => states != null && states.Any(NeedsWriting);

        /// <summary>What Sync would write. A broken marker is not on the list: it needs a person.</summary>
        private bool NeedsWriting(SyncFileState state)
        {
            return state.Status == SyncStatus.Absent || state.Status == SyncStatus.Stale;
        }

        private bool Broken(SyncStatus status)
        {
            return status == SyncStatus.Malformed || status == SyncStatus.Failed;
        }

        /// <summary>
        /// How hard a row is tinted. A settled row is fainter than a row with something to say, so
        /// a project where everything is current does not shout as loudly as the one red row in it.
        /// </summary>
        private float Alpha(SyncStatus status)
        {
            return status == SyncStatus.Current ? FlowRowPainter.QUIET_ALPHA : FlowRowPainter.FILL_ALPHA;
        }

        /// <summary>
        /// Absent and Stale are amber and not red: Sync clears either one, and so does the
        /// automatic pass on the next Editor session. Malformed and Failed are red, because
        /// pressing Sync will not clear them however many times it is pressed.
        /// </summary>
        private Color ColorFor(SyncStatus status)
        {
            switch (status)
            {
                case SyncStatus.Absent:
                case SyncStatus.Stale:
                    return _painter.Warn;
                case SyncStatus.Malformed:
                case SyncStatus.Failed:
                    return _painter.Error;
                default:
                    return _painter.Ok;
            }
        }

        private string IconFor(SyncStatus status)
        {
            switch (status)
            {
                case SyncStatus.Absent:
                case SyncStatus.Stale:
                    return "⚠";
                case SyncStatus.Malformed:
                case SyncStatus.Failed:
                    return "✖";
                default:
                    return "✔";
            }
        }

        private string Describe(SyncFileState state)
        {
            switch (state.Status)
            {
                case SyncStatus.Current: return "up to date";
                case SyncStatus.Absent: return "not installed - press Sync";
                case SyncStatus.Stale: return "out of date - press Sync";
                case SyncStatus.Malformed: return "malformed: " + state.Message;
                default: return "failed: " + state.Message;
            }
        }

        private string Count(int value, string noun) => value == 1 ? $"1 {noun}" : $"{value} {noun}s";
    }
}

#endif