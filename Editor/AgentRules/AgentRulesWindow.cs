#if UNITY_EDITOR

using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.AgentRules
{
    /// <summary>
    /// Shows what the FlowIoC rule block looks like in this project's AGENTS.md and CLAUDE.md,
    /// writes it on request, and carries the switch that decides whether FlowIoC keeps it current
    /// on its own.
    /// </summary>
    internal class AgentRulesWindow : EditorWindow
    {
        [MenuItem("Tools/FlowIoC/AI/Agent Rules", false, -1140)]
        internal static void Open()
        {
            AgentRulesWindow window = GetWindow<AgentRulesWindow>("Agent Rules");
            window.minSize = new Vector2(460, 260);
            window.Show();
        }

        private readonly AgentRulesAutoSync _autoSync = new AgentRulesAutoSync();

        private SyncFileState[] _states;
        private string _projectRoot;

        private void OnEnable()
        {
            _projectRoot = new ProjectRoot().Resolve();
            Refresh();
        }

        private void OnFocus() => Refresh();

        private void OnGUI()
        {
            EditorGUILayout.LabelField("FlowIoC agent rules", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Writes FlowIoC's architecture rules into this project's AGENTS.md so AI coding "
                + "assistants follow them, and points CLAUDE.md at that file. Only the text "
                + "between the FLOWIOC markers is touched - rules you wrote yourself are left alone.",
                MessageType.Info);

            EditorGUILayout.Space();

            if (_states == null || _states.Length == 0)
            {
                EditorGUILayout.LabelField("Nothing to report.");
                return;
            }

            foreach (var state in _states)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(Path.GetFileName(state.Path), GUILayout.Width(120));
                    EditorGUILayout.LabelField(Describe(state));
                }
            }

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Sync", GUILayout.Height(28)))
                {
                    _states = NewSynchronizer().Sync();
                    AssetDatabase.Refresh();
                }

                if (GUILayout.Button("Refresh", GUILayout.Height(28), GUILayout.Width(90)))
                    Refresh();
            }

            EditorGUILayout.Space();
            DrawAutoSyncToggle();
        }

        /// <summary>
        /// FlowIoC writes the block whenever it is absent or stale, without asking. A project that
        /// would rather decide for itself turns that off here, and then nothing is written until
        /// Sync is pressed.
        /// </summary>
        private void DrawAutoSyncToggle()
        {
            bool on = !_autoSync.IsOff(_projectRoot);
            bool wanted = EditorGUILayout.ToggleLeft(
                "Keep AGENTS.md and CLAUDE.md up to date automatically", on);

            if (wanted == on)
                return;

            if (wanted)
                _autoSync.TurnOn(_projectRoot);
            else
                _autoSync.TurnOff(_projectRoot);
        }

        private void Refresh() => _states = NewSynchronizer().Inspect();

        private AgentRulesSynchronizer NewSynchronizer() =>
            new AgentRulesSynchronizer(_projectRoot, new AgentRulesSource());

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
    }
}

#endif