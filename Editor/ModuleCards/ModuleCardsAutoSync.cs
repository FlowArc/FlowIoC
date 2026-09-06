#if UNITY_EDITOR

using FlowIoC.Editor.AgentRules;
using UnityEditor;

namespace FlowIoC.Editor.ModuleCards
{
    /// <summary>
    /// Whether FlowIoC keeps this project's module cards and directory up to date on its own. It
    /// does by default, for the same reason it keeps the rule block current: both are generated
    /// text between two markers, so nothing a reader wrote is ever touched.
    ///
    /// The switch is remembered in EditorPrefs, which is shared by every project a user opens
    /// with the same Editor, so the key carries the project root: turning it off in one project
    /// must not turn it off in every other one.
    /// </summary>
    internal class ModuleCardsAutoSync : IAutoSyncSwitch
    {
        private const string KEY_PREFIX = "FlowIoC.ModuleCards.AutoSyncOff.";
        private const string OFF = "off";

        internal string KeyFor(string projectRoot)
        {
            string normalized = (projectRoot ?? string.Empty)
                .Replace('\\', '/')
                .TrimEnd('/')
                .ToLowerInvariant();

            return KEY_PREFIX + new ManagedBlockWriter().ComputeHash(normalized);
        }

        public bool IsOff(string projectRoot) => EditorPrefs.GetString(KeyFor(projectRoot), string.Empty) == OFF;

        public void TurnOff(string projectRoot) => EditorPrefs.SetString(KeyFor(projectRoot), OFF);

        public void TurnOn(string projectRoot) => EditorPrefs.DeleteKey(KeyFor(projectRoot));
    }
}

#endif
