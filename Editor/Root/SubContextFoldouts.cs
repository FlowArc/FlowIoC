#if UNITY_EDITOR
using UnityEditor;

namespace FlowIoC.Editor.Root
{
    /// <summary>
    /// Whether a Root's sub-context entry, and the screen or settings block inside it, are expanded. A Root
    /// that lists half a dozen screen contexts would otherwise be a wall of fields, so both start
    /// folded.
    ///
    /// The state is keyed by Root and by context, which is what lets two Roots list the same
    /// screen context and fold it separately. It lives in SessionState rather than in this class,
    /// so it survives a selection change and a domain reload and is forgotten when the Editor
    /// closes - a fold is a convenience, not something to persist into the project.
    /// </summary>
    internal class SubContextFoldouts
    {
        private const string Prefix = "FlowIoC.SubContext.";

        internal bool IsEntryExpanded(int rootInstanceId, string contextFullName)
            => SessionState.GetBool(EntryKey(rootInstanceId, contextFullName), false);

        internal void SetEntryExpanded(int rootInstanceId, string contextFullName, bool expanded)
            => SessionState.SetBool(EntryKey(rootInstanceId, contextFullName), expanded);

        internal bool IsScreenExpanded(int rootInstanceId, string contextFullName)
            => SessionState.GetBool(ScreenKey(rootInstanceId, contextFullName), false);

        internal void SetScreenExpanded(int rootInstanceId, string contextFullName, bool expanded)
            => SessionState.SetBool(ScreenKey(rootInstanceId, contextFullName), expanded);

        /// <summary>The settings block of any other configurable sub-context - a module's pool groups.</summary>
        internal bool IsSettingsExpanded(int rootInstanceId, string contextFullName)
            => SessionState.GetBool(SettingsKey(rootInstanceId, contextFullName), false);

        internal void SetSettingsExpanded(int rootInstanceId, string contextFullName, bool expanded)
            => SessionState.SetBool(SettingsKey(rootInstanceId, contextFullName), expanded);

        private string SettingsKey(int rootInstanceId, string contextFullName)
            => $"{Prefix}Settings.{rootInstanceId}.{contextFullName}";

        private string EntryKey(int rootInstanceId, string contextFullName)
            => $"{Prefix}Entry.{rootInstanceId}.{contextFullName}";

        private string ScreenKey(int rootInstanceId, string contextFullName)
            => $"{Prefix}Screen.{rootInstanceId}.{contextFullName}";
    }
}
#endif
