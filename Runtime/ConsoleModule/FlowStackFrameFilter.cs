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

        /// <summary>
        /// Reads the "(at Assets/A.cs:22)" a Unity stack frame ends with. It is the only place a
        /// log taken from Application.logMessageReceived says where it came from, so without this
        /// double-clicking one of Unity's own messages has nothing to open.
        /// </summary>
        public bool TryParseFrame(string traceLine, out string filePath, out int lineNumber)
        {
            filePath = null;
            lineNumber = 0;

            if (string.IsNullOrEmpty(traceLine)) return false;

            int atIndex = traceLine.LastIndexOf("(at ", StringComparison.Ordinal);
            if (atIndex < 0) return false;

            int closeIndex = traceLine.LastIndexOf(')');
            if (closeIndex <= atIndex) return false;

            string inside = traceLine.Substring(atIndex + 4, closeIndex - atIndex - 4);
            int lastColon = inside.LastIndexOf(':');
            if (lastColon < 0) return false;

            if (!int.TryParse(inside.Substring(lastColon + 1), out lineNumber)) return false;

            filePath = inside.Substring(0, lastColon);
            return !string.IsNullOrEmpty(filePath);
        }

        /// <summary>
        /// The file name at the end of a path, without going through System.IO.Path.
        ///
        /// These paths are read out of stack traces and compiler messages, so they are whatever
        /// text happened to be there - a generated frame, a path with a character Windows does
        /// not allow. Path.GetFileName throws ArgumentException on those, and a throw inside
        /// OnGUI unbalances GUILayout and takes the whole window's drawing down with it.
        /// </summary>
        public string FileNameOf(string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;

            int slash = path.LastIndexOfAny(PathSeparators);
            return slash < 0 ? path : path.Substring(slash + 1);
        }

        /// <summary>The file name at the end of a path, with its extension taken off.</summary>
        public string FileNameWithoutExtensionOf(string path)
        {
            string name = FileNameOf(path);

            int dot = name.LastIndexOf('.');
            return dot <= 0 ? name : name.Substring(0, dot);
        }

        private static readonly char[] PathSeparators = {'/', '\\'};

        /// <summary>The class name a Unity stack frame starts with, or null.</summary>
        public string ParseClassName(string traceLine)
        {
            if (string.IsNullOrEmpty(traceLine)) return null;

            int colon = traceLine.IndexOf(':');
            if (colon <= 0) return null;

            string fullName = traceLine.Substring(0, colon);
            int slash = fullName.IndexOf('/');
            return slash > 0 ? fullName.Substring(0, slash) : fullName;
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