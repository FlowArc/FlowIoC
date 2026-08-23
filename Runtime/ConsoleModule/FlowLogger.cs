using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace FlowIoC.ConsoleModule
{
    public static class FlowLogger
    {
        public static readonly List<ConsoleLog> Logs = new();
        public static Action<ConsoleLog> OnLogAdded;

        private const int MaxMessageLength = 15000;
        private const char ArrowDown = '\u21d3';
        private const char ArrowUp = '\u21d1';

        private static FlowConsoleSettings _settings;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Logs.Clear();
            _settings = null;
            // OnLogAdded intentionally NOT cleared: the FlowConsole editor window
            // subscribes once in OnEnable and would otherwise silently lose its
            // subscription on every Play entry when domain reload is disabled.
        }

        public static FlowConsoleSettings Settings
        {
            get
            {
                if (_settings == null)
                {
                    _settings = Resources.Load<FlowConsoleSettings>("FlowConsoleSettings");

                    if (_settings == null)
                    {
                        _settings = ScriptableObject.CreateInstance<FlowConsoleSettings>();
                        _settings.ResetToDefaults();

#if UNITY_EDITOR
                        UnityEditor.EditorApplication.delayCall += () =>
                        {
                            const string resourcesPath = "Assets/Resources";
                            const string fullPath = resourcesPath + "/FlowConsoleSettings.asset";

                            bool fileExistsOnDisk = File.Exists(fullPath);
                            var existing = UnityEditor.AssetDatabase.LoadAssetAtPath<FlowConsoleSettings>(fullPath);

                            if (!new FlowConsoleSettingsCreationPolicy().ShouldCreate(existing != null, fileExistsOnDisk))
                            {
                                if (fileExistsOnDisk && existing == null)
                                {
                                    Debug.LogWarning(
                                        "<color=cyan>FlowConsole:</color> FlowConsoleSettings.asset is on disk but " +
                                        "could not be loaded, so it was left untouched rather than replaced. Scripts " +
                                        "are probably not compiling, or the package's asset paths changed. Fix the " +
                                        "compile errors - or close the Editor, delete Library/ and reopen - and the " +
                                        "settings will load again with your log types intact.");
                                }

                                return;
                            }

                            if (!Directory.Exists(resourcesPath))
                                Directory.CreateDirectory(resourcesPath);

                            UnityEditor.AssetDatabase.CreateAsset(_settings, fullPath);
                            UnityEditor.AssetDatabase.SaveAssets();
                            UnityEditor.AssetDatabase.Refresh();
                            Debug.Log("<color=cyan>FlowConsoleLogger:</color> Created FlowConsoleSettings and verified FlowLogType.");
                        };
#endif
                    }
                }

                return _settings;
            }
        }

        public static void ClearLogs()
        {
            Logs.Clear();
        }

        public static int GetModuleLogType<T>()
        {
            var ns = typeof(T).Namespace;
            if (ns != null)
            {
                var parts = ns.Split('.');
                if (parts.Length >= 2)
                    return GetLogTypeValue(parts[1]);
            }

            return -1;
        }

        public static int GetLogTypeValue(string typeName)
        {
            foreach (var type in Settings.LogTypes)
            {
                if (string.Equals(type.Name, typeName, StringComparison.OrdinalIgnoreCase))
                    return type.Value;
            }

            return -1;
        }

        // ======================== Log ========================

        [HideInCallstack]
        [Conditional("ENABLE_LOG")]
        internal static void Log(SystemLogType systemLogType, string message)
        {
            AddLog(systemLogType, message, LogType.Log);
        }

        [HideInCallstack]
        [Conditional("ENABLE_LOG")]
        public static void Log(int logTypeValue, string message)
        {
            AddCustomLog(logTypeValue, ResolveMessage(logTypeValue, message), LogType.Log);
        }

        [HideInCallstack]
        [Conditional("ENABLE_LOG")]
        public static void Log(int logTypeValue, string message, FlowLogProfile profile)
        {
            string formatted = profile != null ? FormatWithProfile(message, profile) : message;
            AddCustomLog(logTypeValue, formatted, LogType.Log);
        }

        // ======================== LogWarning ========================

        [HideInCallstack]
        [Conditional("ENABLE_LOG")]
        internal static void LogWarning(SystemLogType systemLogType, string message)
        {
            AddLog(systemLogType, message, LogType.Warning);
        }

        [HideInCallstack]
        [Conditional("ENABLE_LOG")]
        public static void LogWarning(int logTypeValue, string message)
        {
            AddCustomLog(logTypeValue, ResolveMessage(logTypeValue, message), LogType.Warning);
        }

        [HideInCallstack]
        [Conditional("ENABLE_LOG")]
        public static void LogWarning(int logTypeValue, string message, FlowLogProfile profile)
        {
            string formatted = profile != null ? FormatWithProfile(message, profile) : message;
            AddCustomLog(logTypeValue, formatted, LogType.Warning);
        }

        // ======================== LogError ========================

        [HideInCallstack]
        [Conditional("ENABLE_LOG")]
        internal static void LogError(SystemLogType systemLogType, string message, string unityMessage = "")
        {
            if (!string.IsNullOrEmpty(unityMessage))
                Debug.LogError(unityMessage);
            else
                Debug.LogError(message);

            if (!Settings.IsLoggingEnabled) return;

            AddLog(systemLogType, message, LogType.Error);
        }

        [HideInCallstack]
        [Conditional("ENABLE_LOG")]
        public static void LogError(int logTypeValue, string message)
        {
            AddCustomLog(logTypeValue, ResolveMessage(logTypeValue, message), LogType.Error);
        }

        [HideInCallstack]
        [Conditional("ENABLE_LOG")]
        public static void LogError(int logTypeValue, string message, FlowLogProfile profile)
        {
            string formatted = profile != null ? FormatWithProfile(message, profile) : message;
            AddCustomLog(logTypeValue, formatted, LogType.Error);
        }

        // ======================== LogLong ========================

        [HideInCallstack]
        [Conditional("ENABLE_LOG")]
        public static void LogLong(int logTypeValue, string message, FlowLogProfile profile = null)
        {
            profile ??= Settings.GetResolvedProfile(logTypeValue);
            LogLongInternal(logTypeValue, message, profile);
        }

        [HideInCallstack]
        private static void LogLongInternal(int logTypeValue, string message, FlowLogProfile profile)
        {
            if (!Settings.IsLoggingEnabled) return;

            int messageLength = message.Length;
            if (messageLength <= MaxMessageLength)
            {
                string formatted = profile != null ? FormatWithProfile(message, profile) : message;
                AddCustomLog(logTypeValue, formatted, LogType.Log);
                return;
            }

            int chunkCount = Mathf.CeilToInt((float) messageLength / MaxMessageLength);

            for (int i = 0; i < chunkCount; i++)
            {
                int startIndex = MaxMessageLength * i;
                int partLength = (startIndex + MaxMessageLength < messageLength)
                    ? MaxMessageLength
                    : messageLength - startIndex;

                string chunk = message.Substring(startIndex, partLength);
                string decorated;

                if (i == 0)
                    decorated = $"{ArrowDown}{ArrowDown}{ArrowDown} {chunk} {ArrowDown}{ArrowDown}{ArrowDown}";
                else if (i < chunkCount - 1)
                    decorated = $"{ArrowUp}{ArrowUp}{ArrowUp} {chunk} {ArrowDown}{ArrowDown}{ArrowDown}";
                else
                    decorated = $"{ArrowUp}{ArrowUp}{ArrowUp} {chunk}";

                if (profile != null)
                {
                    string prefix = (i == 0 && !string.IsNullOrEmpty(profile.Prefix))
                        ? FormatPart(profile.Prefix, profile.PrefixStyle, profile.PrefixColor) + " "
                        : "";

                    string postfix = (i == chunkCount - 1 && !string.IsNullOrEmpty(profile.Postfix))
                        ? " " + FormatPart(profile.Postfix, profile.PostfixStyle, profile.PostfixColor)
                        : "";

                    string styledChunk = FormatPart(decorated, profile.MessageStyle, profile.MessageColor);

                    AddCustomLog(logTypeValue, prefix + styledChunk + postfix, LogType.Log);
                }
                else
                {
                    AddCustomLog(logTypeValue, decorated, LogType.Log);
                }
            }
        }

        // ======================== Internal ========================

        [HideInCallstack]
        private static void AddLog(SystemLogType systemLogType, string message, LogType logType)
        {
            if (!Settings.IsLoggingEnabled) return;

#if UNITY_EDITOR
            var log = CreateLogEntry(message, logType);
            log.SystemLogType = systemLogType;
            log.LogTypeValue = (int) systemLogType;

            if (Settings.TryGetLogType((int) systemLogType, out var typeInfo))
                log.LogColor = typeInfo.LogColor;

            Logs.Add(log);
            OnLogAdded?.Invoke(log);
#endif

            ForwardToUnityConsole((int) systemLogType, message, logType);
        }

        [HideInCallstack]
        private static void AddCustomLog(int logTypeValue, string message, LogType logType)
        {
            if (!Settings.IsLoggingEnabled) return;

#if UNITY_EDITOR
            var log = CreateLogEntry(message, logType);
            log.LogTypeValue = logTypeValue;

            if (Settings.TryGetLogType(logTypeValue, out var typeInfo))
                log.LogColor = typeInfo.LogColor;

            Logs.Add(log);
            OnLogAdded?.Invoke(log);
#endif

            if (logType == LogType.Error)
            {
                Debug.LogError(message);
            }
            else
            {
                ForwardToUnityConsole(logTypeValue, message, logType);
            }
        }

#if UNITY_EDITOR
        private static ConsoleLog CreateLogEntry(string message, LogType logType)
        {
            var now = DateTime.Now;
            var log = new ConsoleLog
            {
                Hour = now.Hour,
                Minute = now.Minute,
                Second = now.Second,
                Millisecond = now.Millisecond,
                Message = message,
                LogType = logType
            };

            GetSourceInfo(log);
            return log;
        }
#endif

        private static void ForwardToUnityConsole(int logTypeValue, string message, LogType logType)
        {
            if (!Settings.SendLogsToUnityConsole || !Settings.IsLogTypeVisible(logTypeValue)) return;

            switch (logType)
            {
                case LogType.Log:
                    Debug.Log(message);
                    break;
                case LogType.Warning:
                    Debug.LogWarning(message);
                    break;
            }
        }

        // ======================== Formatting ========================

        [HideInCallstack]
        private static string ResolveMessage(int logTypeValue, string message)
        {
            var profile = Settings.GetResolvedProfile(logTypeValue);
            return profile != null ? FormatWithProfile(message, profile) : message;
        }

        private static string FormatWithProfile(string message, FlowLogProfile profile)
        {
            string prefix = !string.IsNullOrEmpty(profile.Prefix)
                ? FormatPart(profile.Prefix, profile.PrefixStyle, profile.PrefixColor) + " "
                : "";

            string body = FormatPart(message, profile.MessageStyle, profile.MessageColor);

            string postfix = !string.IsNullOrEmpty(profile.Postfix)
                ? " " + FormatPart(profile.Postfix, profile.PostfixStyle, profile.PostfixColor)
                : "";

            return prefix + body + postfix;
        }

        private static string FormatPart(string text, FlowTextStyle style, Color color)
        {
            string result = ApplyStyle(text, style);
            if (color != Color.white)
                result = ApplyColor(result, color);
            return result;
        }

        private static string ApplyStyle(string text, FlowTextStyle style)
        {
            if (style == FlowTextStyle.None) return text;

            if ((style & FlowTextStyle.Bold) != 0)
                text = $"<b>{text}</b>";
            if ((style & FlowTextStyle.Italic) != 0)
                text = $"<i>{text}</i>";
            if ((style & FlowTextStyle.Underline) != 0)
                text = $"<u>{text}</u>";

            return text;
        }

        private static string ApplyColor(string text, Color color)
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{text}</color>";
        }

        // ======================== Source Info ========================

#if UNITY_EDITOR
        private static void GetSourceInfo(ConsoleLog log)
        {
            string rawTrace = StackTraceUtility.ExtractStackTrace();
            if (string.IsNullOrEmpty(rawTrace))
            {
                log.SourceTrace = "Source information not available.";
                log.StackTrace = log.SourceTrace;
                return;
            }

            string[] lines = rawTrace.Split('\n');
            bool sourceFound = false;
            var filteredTrace = new StringBuilder(512);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrEmpty(line)) continue;

                if (line.StartsWith("FlowIoC.ConsoleModule.", StringComparison.Ordinal) ||
                    line.StartsWith("FlowIoC.Editor.Console.", StringComparison.Ordinal) ||
                    line.StartsWith("UnityEngine.Debug:", StringComparison.Ordinal) ||
                    line.StartsWith("UnityEngine.StackTraceUtility:", StringComparison.Ordinal) ||
                    line.StartsWith("UnityEngine.Logger:", StringComparison.Ordinal) ||
                    line.StartsWith("UnityEngine.DebugLogHandler:", StringComparison.Ordinal) ||
                    line.StartsWith("System.Reflection.", StringComparison.Ordinal) ||
                    line.StartsWith("System.Runtime.CompilerServices.", StringComparison.Ordinal) ||
                    line.StartsWith("System.Threading.", StringComparison.Ordinal) ||
                    line.StartsWith("UnityEngine.Events.", StringComparison.Ordinal))
                    continue;

                if (!sourceFound)
                {
                    int colonIdx = line.IndexOf(':');
                    if (colonIdx > 0)
                    {
                        string fullClassName = line.Substring(0, colonIdx);
                        int slashIdx = fullClassName.IndexOf('/');
                        log.SourceClassName = slashIdx > 0
                            ? fullClassName.Substring(0, slashIdx)
                            : fullClassName;
                    }

                    int atIdx = line.LastIndexOf("(at ", StringComparison.Ordinal);
                    if (atIdx >= 0)
                    {
                        int closeIdx = line.LastIndexOf(')');
                        if (closeIdx > atIdx)
                        {
                            string atContent = line.Substring(atIdx + 4, closeIdx - atIdx - 4);
                            int lastColon = atContent.LastIndexOf(':');
                            if (lastColon >= 0)
                            {
                                log.SourceFilePath = atContent.Substring(0, lastColon);
                                if (int.TryParse(atContent.Substring(lastColon + 1), out int lineNum))
                                    log.SourceLineNumber = lineNum;
                            }
                        }

                        string shortName = !string.IsNullOrEmpty(log.SourceFilePath)
                            ? Path.GetFileName(log.SourceFilePath)
                            : null;
                        if (!string.IsNullOrEmpty(shortName))
                            log.SourceTrace = line.Substring(0, atIdx) + $"(at {shortName}:{log.SourceLineNumber})";
                        else
                            log.SourceTrace = line;
                    }
                    else
                    {
                        log.SourceTrace = line;
                    }

                    sourceFound = true;
                }

                if (filteredTrace.Length > 0)
                    filteredTrace.Append('\n');
                filteredTrace.Append(line);
            }

            if (!sourceFound)
            {
                log.SourceTrace = "Source information not available.";
                log.StackTrace = log.SourceTrace;
                return;
            }

            log.StackTrace = filteredTrace.ToString();
        }
#endif
    }
}