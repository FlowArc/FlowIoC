#if UNITY_EDITOR
namespace FlowIoC.Editor.Console
{
    /// <summary>
    /// What leads a row in the Flow Console. Numbered because the choice is saved as an int in
    /// EditorPrefs, and a value inserted in the middle would read every saved choice back as the
    /// wrong one.
    /// </summary>
    public enum FlowConsoleTimeFormat
    {
        /// <summary>The clock to the second - <c>13:05:23</c> - the way Unity's own console leads a row.</summary>
        Classic = 0,

        /// <summary>The clock with its milliseconds - <c>13:05:23:088</c>. The default.</summary>
        Extended = 1,

        /// <summary>
        /// The frame the log was written in and the gap since the row above - <c>f120  +12ms</c>.
        /// A wall clock answers when something happened; these two answer whether two things
        /// happened together, which is the question a flow raises.
        /// </summary>
        Frame = 2
    }
}
#endif
