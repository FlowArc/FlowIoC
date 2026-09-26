#if UNITY_EDITOR
using FlowIoC.BaseModule.Root;
using FlowIoC.Editor.Inspector;
using UnityEditor;

namespace FlowIoC.Editor.Root
{
    /// <summary>
    /// Whether a Root's sub-context entry, and the screen block inside it, are expanded. A Root
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

        private readonly SessionObjectId _ids = new();

        internal bool IsEntryExpanded(RootBase root, string contextFullName)
            => SessionState.GetBool(EntryKey(root, contextFullName), false);

        internal void SetEntryExpanded(RootBase root, string contextFullName, bool expanded)
            => SessionState.SetBool(EntryKey(root, contextFullName), expanded);

        internal bool IsScreenExpanded(RootBase root, string contextFullName)
            => SessionState.GetBool(ScreenKey(root, contextFullName), false);

        internal void SetScreenExpanded(RootBase root, string contextFullName, bool expanded)
            => SessionState.SetBool(ScreenKey(root, contextFullName), expanded);

        private string EntryKey(RootBase root, string contextFullName)
            => $"{Prefix}Entry.{_ids.For(root)}.{contextFullName}";

        private string ScreenKey(RootBase root, string contextFullName)
            => $"{Prefix}Screen.{_ids.For(root)}.{contextFullName}";
    }
}
#endif
