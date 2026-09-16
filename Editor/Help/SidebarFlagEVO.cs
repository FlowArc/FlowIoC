#if UNITY_EDITOR

namespace FlowIoC.Editor.Help
{
    internal enum SidebarFlagTone
    {
        Update = 0,
        New = 1
    }

    /// <summary>
    /// The word at the right edge of a module's sidebar row, on a colour: UPDATE on green for a
    /// module installed behind the shipped version, NEW on red for one that arrived with the
    /// package version now in the project and is not installed.
    /// </summary>
    internal class SidebarFlagEVO
    {
        internal static readonly SidebarFlagEVO Update = new SidebarFlagEVO("UPDATE", SidebarFlagTone.Update);
        internal static readonly SidebarFlagEVO New = new SidebarFlagEVO("NEW", SidebarFlagTone.New);

        private SidebarFlagEVO(string text, SidebarFlagTone tone)
        {
            Text = text;
            Tone = tone;
        }

        internal string Text { get; }

        internal SidebarFlagTone Tone { get; }
    }
}

#endif
