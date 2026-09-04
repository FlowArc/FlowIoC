#if UNITY_EDITOR

using FlowIoC.Editor.AgentRules;
using UnityEditor;

namespace FlowIoC.Editor.AgentSkills
{
    /// <summary>
    /// Whether FlowIoC keeps the skills it ships up to date on its own. It does by default: a
    /// skill is reference material for an AI assistant rather than a change to the project, and
    /// one describing a version of FlowIoC the project is no longer on helps nobody.
    ///
    /// A project may turn it off, and then nothing is written until somebody presses Install in
    /// the Agent Skills window. The switch is remembered in EditorPrefs, which is shared by every
    /// project a user opens with the same Editor, so the key carries the project root: turning it
    /// off in one project must not turn it off in every other one. This is the same switch the
    /// agent rules carry, kept separate so a project can take one and refuse the other.
    /// </summary>
    internal class AgentSkillsAutoSync
    {
        private const string KeyPrefix = "FlowIoC.AgentSkills.AutoSyncOff.";
        private const string Off = "off";

        internal string KeyFor(string projectRoot)
        {
            string normalized = (projectRoot ?? string.Empty)
                .Replace('\\', '/')
                .TrimEnd('/')
                .ToLowerInvariant();

            return KeyPrefix + new ManagedBlockWriter().ComputeHash(normalized);
        }

        internal bool IsOff(string projectRoot)
        {
            return EditorPrefs.GetString(KeyFor(projectRoot), string.Empty) == Off;
        }

        internal void TurnOff(string projectRoot)
        {
            EditorPrefs.SetString(KeyFor(projectRoot), Off);
        }

        internal void TurnOn(string projectRoot)
        {
            EditorPrefs.DeleteKey(KeyFor(projectRoot));
        }
    }
}

#endif
