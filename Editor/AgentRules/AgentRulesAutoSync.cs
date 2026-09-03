#if UNITY_EDITOR

using UnityEditor;

namespace FlowIoC.Editor.AgentRules
{
    /// <summary>
    /// Whether FlowIoC keeps this project's rule block up to date on its own. It does by default:
    /// the block is generated text between two markers, the way the agent skills are files FlowIoC
    /// owns, and a project that has FlowIoC installed wants both to describe the version it is on.
    ///
    /// A project may turn it off, and then nothing is written until somebody presses Sync in the
    /// Agent Rules window. The switch is remembered in EditorPrefs, which is shared by every
    /// project a user opens with the same Editor, so the key carries the project root: turning it
    /// off in one project must not turn it off in every other one.
    /// </summary>
    internal class AgentRulesAutoSync
    {
        private const string KeyPrefix = "FlowIoC.AgentRules.AutoSyncOff.";
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
