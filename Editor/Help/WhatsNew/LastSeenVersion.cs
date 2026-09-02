#if UNITY_EDITOR
using FlowIoC.Editor.AgentRules;
using UnityEditor;

namespace FlowIoC.Editor.Help.WhatsNew
{
    /// <summary>
    /// The FlowIoC version this reader last saw the notes for.
    ///
    /// It is per person rather than per project: a marker committed with the project would be
    /// ticked by whoever updated the package, and everybody who pulled afterwards would never be
    /// shown what changed. EditorPrefs is shared by every project a Unity install opens, so the
    /// key carries the project it is about.
    /// </summary>
    internal class LastSeenVersion
    {
        private const string KEY_PREFIX = "FlowIoC.WhatsNew.LastSeenVersion.";

        private readonly string _key;

        internal LastSeenVersion() : this(new ProjectRoot().Resolve())
        {
        }

        internal LastSeenVersion(string projectRoot)
        {
            _key = KEY_PREFIX + (projectRoot ?? string.Empty).Replace('\\', '/').ToLowerInvariant();
        }

        internal string Read() => EditorPrefs.GetString(_key, string.Empty);

        internal void Write(string version) => EditorPrefs.SetString(_key, version ?? string.Empty);
    }
}

#endif
