#if UNITY_EDITOR
using FlowIoC.ConsoleModule;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
using UnityEngine;

namespace FlowIoC.Editor.Console
{
    /// <summary>
    /// Clear on Play, Clear on Recompile and Error Pause, the way Unity's own console offers
    /// them. Clear on Build is a build callback and sits in FlowConsoleClearOnBuild below.
    ///
    /// Every switch starts off, so nothing here changes what anybody sees until the toolbar can
    /// turn one on.
    /// </summary>
    [InitializeOnLoad]
    internal static class FlowConsoleAutoClear
    {
        private static readonly FlowConsoleState State = new();
        private static readonly FlowConsoleAutoClearPolicy Policy = new();

        static FlowConsoleAutoClear()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;

            CompilationPipeline.compilationStarted -= OnCompilationStarted;
            CompilationPipeline.compilationStarted += OnCompilationStarted;

            Application.logMessageReceived -= OnUnityLogForErrorPause;
            Application.logMessageReceived += OnUnityLogForErrorPause;
        }

        internal static void ClearFor(FlowConsoleClearTrigger trigger)
        {
            if (!Policy.ShouldClear(trigger, State.ClearOnPlay, State.ClearOnRecompile, State.ClearOnBuild))
                return;

            FlowLogger.ClearLogs();
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode)
                ClearFor(FlowConsoleClearTrigger.EnteringPlayMode);
        }

        private static void OnCompilationStarted(object context)
        {
            ClearFor(FlowConsoleClearTrigger.CompilationStarted);
        }

        private static void OnUnityLogForErrorPause(string condition, string stackTrace, LogType type)
        {
            if (!State.ErrorPause) return;
            if (!EditorApplication.isPlaying || EditorApplication.isPaused) return;
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;

            EditorApplication.isPaused = true;
        }
    }

    internal class FlowConsoleClearOnBuild : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            FlowConsoleAutoClear.ClearFor(FlowConsoleClearTrigger.BuildStarted);
        }
    }
}
#endif
