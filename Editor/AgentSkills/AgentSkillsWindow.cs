#if UNITY_EDITOR

using System.IO;
using FlowIoC.Editor.AgentRules;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.AgentSkills
{
    /// <summary>
    /// Shows which of the skills FlowIoC ships are installed in this project, installs them on
    /// request, and carries the switch that decides whether FlowIoC keeps them current on its own.
    /// </summary>
    internal class AgentSkillsWindow : EditorWindow
    {
        [MenuItem("Tools/FlowIoC/AI/Agent Skills", false, -1139)]
        internal static void Open()
        {
            AgentSkillsWindow window = GetWindow<AgentSkillsWindow>("Agent Skills");
            window.minSize = new Vector2(460, 240);
            window.Show();
        }

        private readonly AgentSkillsAutoSync _autoSync = new AgentSkillsAutoSync();

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
            EditorGUILayout.LabelField("FlowIoC agent skills", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "FlowIoC installs the skills it ships into this project's " + AgentSkillsInstaller.TargetFolder
                + " folder when the Editor opens, so an AI coding assistant can load them on demand. "
                + "This window is for checking what is there and for putting a deleted one back. "
                + "One folder per skill; skills you wrote yourself are left alone, and removing "
                + "FlowIoC takes the shipped ones with it.",
                MessageType.Info);

            EditorGUILayout.Space();

            if (_states == null || _states.Length == 0)
            {
                EditorGUILayout.LabelField("This version of FlowIoC ships no skills.");
                return;
            }

            foreach (var state in _states)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(Path.GetFileName(state.Path), GUILayout.Width(200));
                    EditorGUILayout.LabelField(Describe(state));
                }
            }

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Install", GUILayout.Height(28)))
                {
                    _states = NewInstaller().Install();
                    AssetDatabase.Refresh();
                }

                if (GUILayout.Button("Refresh", GUILayout.Height(28), GUILayout.Width(90)))
                    Refresh();
            }

            EditorGUILayout.Space();
            DrawAutoSyncToggle();
        }

        /// <summary>
        /// FlowIoC installs a skill whenever it is absent or stale, without asking. A project that
        /// would rather decide for itself turns that off here, and then nothing is written until
        /// Install is pressed.
        /// </summary>
        private void DrawAutoSyncToggle()
        {
            bool on = !_autoSync.IsOff(_projectRoot);
            bool wanted = EditorGUILayout.ToggleLeft(
                "Keep the shipped skills up to date automatically", on);

            if (wanted == on)
                return;

            if (wanted)
                _autoSync.TurnOn(_projectRoot);
            else
                _autoSync.TurnOff(_projectRoot);
        }

        private void Refresh() => _states = NewInstaller().Inspect();

        private AgentSkillsInstaller NewInstaller() =>
            new AgentSkillsInstaller(_projectRoot, new AgentSkillsSource());

        private string Describe(SyncFileState state)
        {
            switch (state.Status)
            {
                case SyncStatus.Current: return "up to date";
                case SyncStatus.Absent: return "not installed - press Install";
                case SyncStatus.Stale: return "out of date - press Install";
                default: return "failed: " + state.Message;
            }
        }
    }
}

#endif
