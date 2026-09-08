#if UNITY_EDITOR
using FlowIoC.ConsoleModule;

namespace FlowIoC.Editor.Console
{
    /// <summary>
    /// Where the console draws a line across the list. A list that holds an editor session and a
    /// play session at once runs the two together, and the first log of the run then reads as the
    /// last log of the edit. The boundary is wherever a row was written on the other side of the
    /// play button from the row above it.
    /// </summary>
    public class FlowConsoleSessionRule
    {
        public bool StartsSession(ConsoleLog previous, ConsoleLog current)
        {
            // The top of the list is not a boundary; there is nothing above it to leave.
            if (previous == null || current == null) return false;

            return previous.InPlayMode != current.InPlayMode;
        }

        public string Label(ConsoleLog log)
        {
            return log != null && log.InPlayMode ? "Play mode" : "Edit mode";
        }
    }
}
#endif
