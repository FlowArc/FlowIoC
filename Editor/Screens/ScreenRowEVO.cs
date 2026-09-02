#if UNITY_EDITOR
using FlowIoC.BaseModule.Root;
using FlowIoC.ScreenModule.Data;

namespace FlowIoC.Editor.Screens
{
    /// <summary>
    /// One screen context on one Root, as the Screens panel knows it. Root and EntryIndex together
    /// address the SubContextData the row writes back to; the name is checked against the entry
    /// before every write, because the list can change between a scan and an edit.
    /// </summary>
    internal class ScreenRowEVO
    {
        internal RootBase Root;
        internal int EntryIndex;
        internal string ContextFullName;
        internal string ContextName;
        internal string SceneName;

        /// <summary>What the context declares. Null when it could not be read.</summary>
        internal ScreenCVO Declaration;

        /// <summary>
        /// What this Root registers: the entry's override, or the declaration. Null only when
        /// there is neither - an unreadable declaration on an entry that does not override.
        /// </summary>
        internal ScreenCVO Effective;

        internal bool IsOverridden;

        /// <summary>Why the declaration could not be read, for the row's warning. Null when it was.</summary>
        internal string DeclarationError;
    }
}
#endif
