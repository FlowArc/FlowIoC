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

        public bool ErrorPause
        {
            get => EditorPrefs.GetBool(PREFIX + nameof(ErrorPause), false);
            set => EditorPrefs.SetBool(PREFIX + nameof(ErrorPause), value);
        }
    }
}
#endif