#if UNITY_EDITOR
using UnityEditor;

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

        public bool ErrorPause
        {
            get => EditorPrefs.GetBool(PREFIX + nameof(ErrorPause), false);
            set => EditorPrefs.SetBool(PREFIX + nameof(ErrorPause), value);
        }
    }
}
#endif
