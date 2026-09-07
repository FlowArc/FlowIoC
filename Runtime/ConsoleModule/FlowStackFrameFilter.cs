using System;

namespace FlowIoC.ConsoleModule
{
    /// <summary>
    /// Picks the frame a reader has to act on out of a stack trace. A FlowIoC diagnostic is
    /// written by the framework but caused by the game, so every frame belonging to the
    /// framework - and to Unity's own plumbing underneath it - is stepped over, and the first
    /// frame outside is what double-clicking the log opens.
    ///
    /// It reads text rather than reflecting, because StackTraceUtility.ExtractStackTrace hands
    /// back a string and building real frames would cost more than the capture already does.
    /// The package's asmdef sets rootNamespace to FlowIoC and a game's types sit under
    /// Modules.*, so the prefix separates them. A game type deliberately placed under a
    /// FlowIoC namespace would be read as the framework's; that is documented rather than
    /// defended against.
    /// </summary>
    public class FlowStackFrameFilter
    {
        private static readonly string[] SkippedPrefixes =
        {
            "FlowIoC.",
            "UnityEngine.Debug:",
            "UnityEngine.Logger:",
            "UnityEngine.DebugLogHandler:",
            "UnityEngine.StackTraceUtility:",
            "UnityEngine.Events.",
            "System.Reflection.",
            "System.Runtime.CompilerServices.",
            "System.Threading."
        };

        public bool IsFrameworkFrame(string traceLine)
        {
            if (string.IsNullOrEmpty(traceLine)) return true;

            for (int i = 0; i < SkippedPrefixes.Length; i++)
            {
                if (traceLine.StartsWith(SkippedPrefixes[i], StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        /// <summary>The index of the first frame the game owns, or -1 when there is none.</summary>
        public int FindFirstGameFrame(string[] traceLines)
        {
            if (traceLines == null) return -1;

            for (int i = 0; i < traceLines.Length; i++)
            {
                if (!IsFrameworkFrame(traceLines[i]))
                    return i;
            }

            return -1;
        }
    }
}
