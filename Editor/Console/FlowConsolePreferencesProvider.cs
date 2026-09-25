#if UNITY_EDITOR
using System.Collections.Generic;
using FlowIoC.ConsoleModule;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.Console
{
    /// <summary>
    /// Preferences ▸ FlowIoC ▸ Flow Console: how much the logger does, as one developer wants it.
    /// These are EditorPrefs, so Unity's own Preferences window is where they belong - the same
    /// place a developer's other per-machine choices live, searchable by the words on the page.
    /// What changes while a flow is being read - the Source capture - is also on the console's
    /// own bar, where the reading happens.
    ///
    /// The provider is the one static Unity forces; everything it draws reads and writes
    /// <see cref="FlowLogger.Preferences"/>.
    /// </summary>
    internal static class FlowConsolePreferencesProvider
    {
        private const string PATH = "Preferences/FlowIoC/Flow Console";

        [SettingsProvider]
        private static SettingsProvider Create()
        {
            return new SettingsProvider(PATH, SettingsScope.User)
            {
                label = "Flow Console",
                guiHandler = _ => Draw(),
                keywords = new HashSet<string>(new[]
                {
                    "FlowIoC", "Flow Console", "log", "logging", "console", "stack trace", "source", "capture",
                    "mirror", "Unity console", "max log count", "deep analysis"
                })
            };
        }

        private static void Draw()
        {
            FlowConsolePreferences preferences = FlowLogger.Preferences;

            EditorGUIUtility.labelWidth = 220f;
            EditorGUILayout.Space(8f);

            preferences.IsLoggingEnabled = EditorGUILayout.Toggle(
                new GUIContent("Logging enabled",
                    "The master switch. Off, nothing is recorded and nothing is mirrored. A warning and an error still reach Unity's console."),
                preferences.IsLoggingEnabled);

            preferences.SendLogsToUnityConsole = EditorGUILayout.Toggle(
                new GUIContent("Mirror into Unity's console",
                    "Send the plain logs to Unity's own console as well, for the two side by side - each only while its channel is on. Warnings and errors reach Unity's console whether this is on or off."),
                preferences.SendLogsToUnityConsole);

            preferences.StackTraceCapture = (FlowStackTraceCapture) EditorGUILayout.EnumPopup(
                new GUIContent("Source capture",
                    "Which logs work out where they came from. Capturing a source builds the whole managed stack as a string, and the framework logs every signal, injection and command - so this is the most expensive thing the console does. Warnings and errors are what it is spent on by default; raise it to Always while following a flow. Also on the console's bar, as Source."),
                preferences.StackTraceCapture);

            preferences.MaxLogCount = EditorGUILayout.IntField(
                new GUIContent("Max log count",
                    "How many logs the console keeps. The oldest are dropped past this. Zero keeps all of them."),
                preferences.MaxLogCount);

            preferences.DeepAnalysis = EditorGUILayout.Toggle(
                new GUIContent("Deep analysis in the detail panel",
                    "Show the class and the whole stack trace under a selected row, rather than only the source line."),
                preferences.DeepAnalysis);

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField(
                "Logging compiles only in the Editor and in a Development Build; a release build carries none of it, "
                + "and there is no scripting define to manage. Which channels show is the Filters panel's, and a "
                + "module's colour is the module's own, written in its MODULE.md.",
                EditorStyles.wordWrappedMiniLabel);
        }
    }
}
#endif
