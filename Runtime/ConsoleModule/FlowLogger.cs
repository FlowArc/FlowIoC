using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;
#if UNITY_EDITOR
using FlowIoC.BaseModule.ProjectPaths;
#endif

namespace FlowIoC.ConsoleModule
{
    public static class FlowLogger
    {
        public static readonly List<ConsoleLog> Logs = new();
        public static Action<ConsoleLog> OnLogAdded;

        private const int MaxMessageLength = 15000;
        private const int LogTrimChunk = 256;

        private const string NotCaptured =
            "Source not captured. Raise Stack Trace Capture in the Flow Console settings to see it.";

        private const char ArrowDown = '\u21d3';
        private const char ArrowUp = '\u21d1';

        private static CD_FlowConsole _settings;

        private static readonly CollapseKeyBuilder CollapseKeys = new();
        private static readonly FlowStackFrameFilter StackFrames = new();

        /// <summary>
        /// True while a log of ours is being handed to Unity's console. The editor bridge reads
        /// it to drop the message Unity hands straight back, so a log that made that round trip
        /// is recorded once rather than twice.
        /// </summary>
        public static bool IsWritingToUnityConsole { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Logs.Clear();
            _settings = null;
            IsWritingToUnityConsole = false;
            // OnLogAdded intentionally NOT cleared: the FlowConsole editor window
            // subscribes once in OnEnable and would otherwise silently lose its
            // subscription on every Play entry when domain reload is disabled.
        }

        public static CD_FlowConsole Settings
        {
            get
            {
                if (_settings == null)
                {
                    _settings = Resources.Load<CD_FlowConsole>("CD_FlowConsole");

                    if (_settings == null)
                    {
                        _settings = ScriptableObject.CreateInstance<CD_FlowConsole>();
                        _settings.ResetToDefaults();
                        _settings.IsStandIn = true;

#if UNITY_EDITOR
                        UnityEditor.EditorApplication.delayCall += () =>
                        {
                            var paths = new FlowIoCProjectPaths();
                            string resourcesPath = paths.ResourcesRoot;
                            string fullPath = paths.ConsoleSettings;

                            bool fileExistsOnDisk = File.Exists(fullPath);
                            var existing = UnityEditor.AssetDatabase.LoadAssetAtPath<CD_FlowConsole>(fullPath);

                            if (!new FlowConsoleSettingsCreationPolicy().ShouldCreate(existing != null, fileExistsOnDisk))
                            {
                                if (fileExistsOnDisk && existing == null)
                                {
                                    Debug.LogWarning(
                                        "<color=cyan>FlowConsole:</color> CD_FlowConsole.asset is on disk but " +
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
                            _settings.IsStandIn = false;
                            UnityEditor.AssetDatabase.SaveAssets();
                            UnityEditor.AssetDatabase.Refresh();
                            Debug.Log("<color=cyan>FlowConsoleLogger:</color> Created CD_FlowConsole and verified FlowLogType.");
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

        /// <summary>
        /// Whether a log written now would be kept. For a call site that pays to build its message -
        /// an interpolation, an enum's name - and sits on a hot path: <c>[Conditional]</c> only removes
        /// the call when the define is absent, and a project that defines it still builds every
        /// message with logging switched off.
        /// </summary>
        public static bool IsEnabled => Settings.IsLoggingEnabled;

        // ======================== Log ========================

        [HideInCallstack]
        [Conditional("ENABLE_LOG")]
        internal static void Log(SystemLogType systemLogType, string message)
        {
            AddLog(systemLogType, message, LogType.Log);
        }

        /// <summary>
        /// The parts of a message, joined only when logging is on. The hot paths - a dispatch, a
        /// command, a function - log through these so that logging switched off costs them nothing
        /// but the call.
        /// </summary>
        [HideInCallstack]
        [Conditional("ENABLE_LOG")]
        internal static void Log(SystemLogType systemLogType, string part1, string part2)
        {
            if (!Settings.IsLoggingEnabled) return;
            AddLog(systemLogType, part1 + part2, LogType.Log);
        }

        [HideInCallstack]
        [Conditional("ENABLE_LOG")]
        internal static void Log(SystemLogType systemLogType, string part1, string part2, string part3)
        {
            if (!Settings.IsLoggingEnabled) return;
            AddLog(systemLogType, part1 + part2 + part3, LogType.Log);
        }

        [HideInCallstack]
        [Conditional("ENABLE_LOG")]
        internal static void Log(SystemLogType systemLogType, string part1, string part2, string part3, string part4)
        {
            if (!Settings.IsLoggingEnabled) return;
            AddLog(systemLogType, part1 + part2 + part3 + part4, LogType.Log);
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

        /// <summary>
        /// A framework warning that names the type it is about. Double-clicking it opens that
        /// type's script, which matters when the offending object was never on the stack - an
        /// asynchronous release, or a resolver noticing a step later than it happened.
        /// </summary>
        [HideInCallstack]
        [Conditional("ENABLE_LOG")]
        internal static void LogWarning(SystemLogType systemLogType, string message, Type blame)
        {
            AddLog(systemLogType, message, LogType.Warning, blame);
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
        internal static void LogError(SystemLogType systemLogType, string message, string unityMessage = "",
            UnityEngine.Object context = null)
        {
            WriteError((int) systemLogType, systemLogType, message, unityMessage, context);
        }

        /// <summary>
        /// A framework error that names the type it is about, so the log points at the code
        /// whose author has to change something rather than at the guard clause that caught it.
        /// </summary>
        [HideInCallstack]
        internal static void LogError(SystemLogType systemLogType, string message, Type blame,
            string unityMessage = "", UnityEngine.Object context = null)
        {
            WriteError((int) systemLogType, systemLogType, message, unityMessage, context, blame);
        }

        [HideInCallstack]
        public static void LogError(int logTypeValue, string message, UnityEngine.Object context = null)
        {
            WriteError(logTypeValue, null, ResolveMessage(logTypeValue, message), null, context);
        }

        [HideInCallstack]
        public static void LogError(int logTypeValue, string message, FlowLogProfile profile,
            UnityEngine.Object context = null)
        {
            string formatted = profile != null ? FormatWithProfile(message, profile) : message;
            WriteError(logTypeValue, null, formatted, null, context);
        }

        /// <summary>
        /// The one path an error takes. It carries no [Conditional] attribute and consults no setting,
        /// because a project that never defined ENABLE_LOG - or turned logging off - is exactly the one
        /// that has to be told something is broken. Every error is written here, and written once.
        /// </summary>
        [HideInCallstack]
        private static void WriteError(int logTypeValue, SystemLogType? systemLogType, string message,
            string unityMessage, UnityEngine.Object context, Type blame = null)
        {
#if UNITY_EDITOR
            var log = CreateLogEntry(message, LogType.Error, blame);
            log.LogTypeValue = logTypeValue;

            if (systemLogType.HasValue)
                log.SystemLogType = systemLogType.Value;

            if (Settings.TryGetLogType(logTypeValue, out var typeInfo))
                log.LogColor = typeInfo.LogColor;

            AppendLog(log);
#endif

            // Unlike an ordinary log, an error always reaches Unity's console - it is not gated
            // on SendLogsToUnityConsole. The flag is raised anyway, because the editor bridge
            // would otherwise take this back through Application.logMessageReceived and record
            // the error a second time.
            IsWritingToUnityConsole = true;
            try
            {
                Debug.LogError(string.IsNullOrEmpty(unityMessage) ? message : unityMessage, context);
            }
            finally
            {
                IsWritingToUnityConsole = false;
            }
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

        // ======================== External intake ========================

        /// <summary>
        /// The door Unity's own logs come in through. It carries no [Conditional] attribute and
        /// consults no setting, because a developer who turned logging off - or never defined
        /// ENABLE_LOG - still has to be able to read Unity's console in this window. That is the
        /// whole promise of Flow Console being the console rather than a second one.
        /// Only the editor bridge calls it.
        /// </summary>
        public static void AddExternalLog(LogSource source, LogType logType, string message,
            string stackTrace, string filePath, int lineNumber)
        {
#if UNITY_EDITOR
            int channel = source == LogSource.Compiler
                ? (int) SystemLogType.Compiler
                : (int) SystemLogType.Unity;

            var now = DateTime.Now;
            var log = new ConsoleLog
            {
                Hour = now.Hour,
                Minute = now.Minute,
                Second = now.Second,
                Millisecond = now.Millisecond,
                Message = message,
                LogType = logType,
                Source = source,
                LogTypeValue = channel,
                SystemLogType = (SystemLogType) channel,
                StackTrace = stackTrace,
                SourceTrace = stackTrace,
                SourceFilePath = filePath,
                SourceLineNumber = lineNumber,
                Frame = Time.frameCount,
                Realtime = Time.realtimeSinceStartup
            };

            if (Settings.TryGetLogType(channel, out var typeInfo))
                log.LogColor = typeInfo.LogColor;

            AppendLog(log);
#endif
        }

        // ======================== Internal ========================

        [HideInCallstack]
        private static void AddLog(SystemLogType systemLogType, string message, LogType logType, Type blame = null)
        {
            if (!Settings.IsLoggingEnabled) return;

#if UNITY_EDITOR
            var log = CreateLogEntry(message, logType, blame);
            log.SystemLogType = systemLogType;
            log.LogTypeValue = (int) systemLogType;

            if (Settings.TryGetLogType((int) systemLogType, out var typeInfo))
                log.LogColor = typeInfo.LogColor;

            AppendLog(log);
#endif

            ForwardToUnityConsole((int) systemLogType, message, logType);
        }

        [HideInCallstack]
        private static void AddCustomLog(int logTypeValue, string message, LogType logType, Type blame = null)
        {
            if (!Settings.IsLoggingEnabled) return;

#if UNITY_EDITOR
            var log = CreateLogEntry(message, logType, blame);
            log.LogTypeValue = logTypeValue;

            if (Settings.TryGetLogType(logTypeValue, out var typeInfo))
                log.LogColor = typeInfo.LogColor;

            AppendLog(log);
#endif

            ForwardToUnityConsole(logTypeValue, message, logType);
        }

#if UNITY_EDITOR
        private static ConsoleLog CreateLogEntry(string message, LogType logType, Type blame = null)
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

            if (CapturesSourceFor(logType))
                GetSourceInfo(log);
            else
                log.SourceTrace = log.StackTrace = NotCaptured;

            log.Frame = Time.frameCount;
            log.Realtime = Time.realtimeSinceStartup;
            log.BlameTypeName = blame?.FullName;

            return log;
        }

        private static bool CapturesSourceFor(LogType logType)
        {
            switch (Settings.StackTraceCapture)
            {
                case FlowStackTraceCapture.Always:
                    return true;

                case FlowStackTraceCapture.WarningsAndErrors:
                    return logType != LogType.Log;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Keeps the console's own list to the size the settings ask for. Trimmed in one block once
        /// it has run past the limit rather than one entry per log, because dropping the front of a
        /// list moves everything behind it.
        /// </summary>
        private static void AppendLog(ConsoleLog log)
        {
            log.CollapseKey = CollapseKeys.Build(log.Message, log.StackTrace, log.LogTypeValue);

            Logs.Add(log);
            OnLogAdded?.Invoke(log);

            int maxLogCount = Settings.MaxLogCount;
            if (maxLogCount <= 0 || Logs.Count <= maxLogCount + LogTrimChunk)
                return;

            Logs.RemoveRange(0, Logs.Count - maxLogCount);
        }
#endif

        private static void ForwardToUnityConsole(int logTypeValue, string message, LogType logType)
        {
            if (!Settings.SendLogsToUnityConsole || !Settings.IsLogTypeVisible(logTypeValue)) return;

            // Raised so the editor bridge can tell this log apart from somebody else's when
            // Unity hands it straight back through Application.logMessageReceived.
            IsWritingToUnityConsole = true;
            try
            {
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
            finally
            {
                IsWritingToUnityConsole = false;
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

                // Every frame the framework owns is stepped over while looking for the source,
                // so a diagnostic points at the game's code rather than at the guard clause that
                // caught it. Once the source is found the frames go back into the trace: the
                // detail panel shows the whole stack, because somebody chasing a bug in FlowIoC
                // itself needs it. Only the frame the log points at changes.
                bool isFrameworkFrame = StackFrames.IsFrameworkFrame(line);
                if (isFrameworkFrame && !sourceFound)
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