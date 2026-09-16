#if UNITY_EDITOR
using FlowIoC.Editor.AgentRules;
using UnityEditor;

namespace FlowIoC.Editor.Console
{
    /// <summary>
    /// Whether this reader has been handed the Flow Console in this project. The tab goes beside
    /// Unity's Console once - on the project's first meeting with FlowIoC, or on the first update
    /// that brings the tab to a project that has been on FlowIoC for a while - and never again
    /// by itself: a reader who closed it has decided, and the menu item is the door after that.
    ///
    /// Per person rather than per project, for the reason <c>LastSeenVersion</c> gives: a marker
    /// committed with the project would be set by whoever updated the package, and everybody who
    /// pulled afterwards would never get the tab. EditorPrefs is shared by every project a Unity
    /// install opens, so the key carries the project it is about.
    /// </summary>
    internal class FlowConsoleDockRecord
    {
        private const string KEY_PREFIX = "FlowIoC.Console.Docked.";

        private readonly string _key;

        internal FlowConsoleDockRecord() : this(new ProjectRoot().Resolve())
        {
        }

        internal FlowConsoleDockRecord(string projectRoot)
        {
            _key = KEY_PREFIX + (projectRoot ?? string.Empty).Replace('\\', '/').ToLowerInvariant();
        }

        internal bool Read() => EditorPrefs.GetBool(_key, false);

        internal void Write() => EditorPrefs.SetBool(_key, true);
    }
}

#endif
