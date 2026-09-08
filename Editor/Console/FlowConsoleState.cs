#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.Console
{
    /// <summary>
    /// The console's own switches, as one developer set them. They are in EditorPrefs rather
    /// than in CD_FlowConsole because that asset is committed, and a toolbar toggle is nobody
    /// else's business.
    /// </summary>
    public class FlowConsoleState
    {
        private const string PREFIX = "FlowIoC.Console.";

        public bool ClearOnPlay
        {
            get => EditorPrefs.GetBool(PREFIX + nameof(ClearOnPlay), false);
            set => EditorPrefs.SetBool(PREFIX + nameof(ClearOnPlay), value);
        }

        public bool ClearOnRecompile
        {
            get => EditorPrefs.GetBool(PREFIX + nameof(ClearOnRecompile), false);
            set => EditorPrefs.SetBool(PREFIX + nameof(ClearOnRecompile), value);
        }

        public bool ClearOnBuild
        {
            get => EditorPrefs.GetBool(PREFIX + nameof(ClearOnBuild), false);
            set => EditorPrefs.SetBool(PREFIX + nameof(ClearOnBuild), value);
        }

        /// <summary>Whether equal rows are folded into one with a count.</summary>
        public bool Collapse
        {
            get => EditorPrefs.GetBool(PREFIX + nameof(Collapse), false);
            set => EditorPrefs.SetBool(PREFIX + nameof(Collapse), value);
        }

        /// <summary>
        /// How many lines a row shows. Two by default, which is what Unity's console does: the
        /// message, and underneath it where the message came from.
        /// </summary>
        public int RowLineCount
        {
            get => Mathf.Clamp(EditorPrefs.GetInt(PREFIX + nameof(RowLineCount), 2), 1, 3);
            set => EditorPrefs.SetInt(PREFIX + nameof(RowLineCount), Mathf.Clamp(value, 1, 3));
        }

        /// <summary>
        /// Whether a row leads with the frame it was written in and the gap since the row above,
        /// instead of the wall clock.
        /// </summary>
        public bool Timing
        {
            get => EditorPrefs.GetBool(PREFIX + nameof(Timing), false);
            set => EditorPrefs.SetBool(PREFIX + nameof(Timing), value);
        }

        /// <summary>
        /// Whether the list is grouped into the flows the logs belong to, rather than read
        /// straight down in the order they arrived.
        /// </summary>
        public bool FlowMode
        {
            get => EditorPrefs.GetBool(PREFIX + nameof(FlowMode), false);
            set => EditorPrefs.SetBool(PREFIX + nameof(FlowMode), value);
        }

        public bool ErrorPause
        {
            get => EditorPrefs.GetBool(PREFIX + nameof(ErrorPause), false);
            set => EditorPrefs.SetBool(PREFIX + nameof(ErrorPause), value);
        }
    }
}
#endif