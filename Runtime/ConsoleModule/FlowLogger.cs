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

        private static readonly ConsoleLogTrimmer Trimmer = new();
        public static Action<ConsoleLog> OnLogAdded;

        /// <summary>
        /// Raised when the list is emptied. The console window keeps its own copy of the logs, so
        /// without this a clear that came from anywhere but its own button - Clear on Play, Clear
        /// on Recompile - would leave the window showing logs that no longer exist.
        /// </summary>
        public static Action OnLogsCleared;

        private const int MaxMessageLength = 15000;
        private const int LogTrimChunk = 256;

        private const string NotCaptured =
            "Source not captured. Raise Stack Trace Capture in the Flow Console settings to see it.";

        private const char ArrowDown = '\u21d3';
        private const char ArrowUp = '\u21d1';

        private static CD_FlowConsole _settings;

        private static readonly CollapseKeyBuilder CollapseKeys = new();
        private static readonly FlowStackFrameFilter StackFrames = new();

        private static int _flowCounter;
        private static int _currentFlowId;
        private static int _currentParentFlowId;

        private static string _currentDeclarationFile;
        private static int _currentDeclarationLine;

        /// <summary>
        /// The flow being executed right now, which every log written meanwhile belongs to.
        /// A plain static on a main-thread assumption - FlowLogger.Logs already makes that
        /// assumption, since Logs.Add is unguarded. A log written from a background thread
        /// carries 0 and reads as a root.
        /// </summary>
        public static int CurrentFlowId => _currentFlowId;

        /// <summary>The flow the current one was started from. 0 at a root.</summary>
        public static int CurrentParentFlowId => _currentParentFlowId;

        /// <summary>
        /// Takes the next flow id and records the flow it was started from. Returns nothing so
        /// that it can carry [Conditional]: with ENABLE_LOG undefined the call and its
        /// arguments are removed, and a shipping build pays nothing for the console's tree.
        /// A caller must initialise the variables it passes, because nothing assigns them then.
        /// </summary>
        [Conditional("ENABLE_LOG")]
        public static void NextFlowId(ref int flowId, ref int parentFlowId)
        {
            flowId = ++_flowCounter;
            parentFlowId = _currentFlowId;
        }

        /// <summary>
        /// Makes a flow current, handing back the flow and parent it displaced. The parent is
        /// carried rather than derived, so a log written three steps into a chain names the
        /// same parent as the one written at its start.
        /// </summary>
        [Conditional("ENABLE_LOG")]
        public static void EnterFlow(int flowId, int parentFlowId, ref int previousFlowId,
            ref int previousParentFlowId)
        {
            previousFlowId = _currentFlowId;
            previousParentFlowId = _currentParentFlowId;
            _currentFlowId = flowId;
            _currentParentFlowId = parentFlowId;
        }

        /// <summary>
        /// Copies the flow that is current into a caller's own fields, for something that runs
        /// inside a flow somebody else started and has to re-enter it later.
        /// </summary>
        [Conditional("ENABLE_LOG")]
        public static void CaptureCurrentFlow(ref int flowId, ref int parentFlowId)
        {
            flowId = _currentFlowId;
            parentFlowId = _currentParentFlowId;
        }

        /// <summary>Puts back what EnterFlow displaced.</summary>
        [Conditional("ENABLE_LOG")]
        public static void ExitFlow(int previousFlowId, int previousParentFlowId)
        {
            _currentFlowId = previousFlowId;
            _currentParentFlowId = previousParentFlowId;
        }

        /// <summary>
        /// Where the sequence that is running now was declared - the Bind line in the Context. A
        /// signal dispatched from inside a command is dispatched by that sequence, so this is the
        /// line worth opening: the stack at that moment names whatever started the chain, which is
        /// further away and says less.
        ///
        /// It also costs nothing to have. The location is what the compiler wrote into the Bind
        /// call, and using it means a dispatch inside a group builds no stack at all.
        /// </summary>
        [Conditional("ENABLE_LOG")]
        public static void EnterDeclaration(string file, int line, ref string previousFile, ref int previousLine)
        {
            previousFile = _currentDeclarationFile;
            previousLine = _currentDeclarationLine;
            _currentDeclarationFile = file;
            _currentDeclarationLine = line;
        }

        [Conditional("ENABLE_LOG")]
        public static void ExitDeclaration(string previousFile, int previousLine)
        {
            _currentDeclarationFile = previousFile;
            _currentDeclarationLine = previousLine;
        }

        [Conditional("ENABLE_LOG")]
        public static void CaptureCurrentDeclaration(ref string file, ref int line)
        {
            file = _currentDeclarationFile;
            line = _currentDeclarationLine;
        }

        /// <summary>
        /// True while a log of ours is being handed to Unity's console. The editor bridge reads
        /// it to drop the message Unity hands straight back, so a log that made that round trip
        /// is recorded once rather than twice.
        /// </summary>
        public static bool IsWritingToUnityConsole { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            // Logs intentionally NOT cleared. What the console holds when a run starts is Clear on
            // Play's decision and nobody else's - wiping the list here threw away the rows the
            // reader had pinned, and did it after they had been carried across the domain reload.
            _settings = null;
            IsWritingToUnityConsole = false;
            _flowCounter = 0;
            _currentFlowId = 0;
            _currentParentFlowId = 0;
            _currentDeclarationFile = null;
            _currentDeclarationLine = 0;
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
            OnLogsCleared?.Invoke();
        }

        /// <summary>
        /// Empties the console but leaves what the reader pinned. This is what the automatic
        /// clears use - entering play mode, recompiling, starting a build - because those clear to
        /// get the noise of the last run out of the way, and a pinned log is the reader saying
        /// this one is not noise. The Clear button still empties everything, because pressing it
        /// is somebody asking for exactly that.
        /// </summary>
        public static void ClearLogsKeepingPinned()
        {
            for (int i = Logs.Count - 1; i >= 0; i--)
            {
                if (!Logs[i].Pinned) Logs.RemoveAt(i);
            }

            OnLogsCleared?.Invoke();
        }

        /// <summary>
        /// The channel a type's own module logs on, worked out from its namespace:
        /// <c>Modules.Player.Models</c> answers <c>PlayerModule</c>. Null when the project has no
        /// such channel, which is what a caller checks rather than a magic number.
        /// </summary>
        public static string GetModuleLogType<T>()
        {
            var ns = typeof(T).Namespace;
            if (ns == null) return null;

            var parts = ns.Split('.');
            if (parts.Length < 2) return null;

            return FindChannel(parts[1]) ?? FindChannel(parts[1] + "Module");
        }

        /// <summary>
        /// The channel's own name back, if the project has one by that name, and null if it does
        /// not. What it is for is answering with the string the settings hold rather than the one
        /// the caller typed, so a channel found case-insensitively is still logged on under the
        /// spelling everything else uses.
        /// </summary>
        public static string FindChannel(string channel)
        {
            if (string.IsNullOrEmpty(channel)) return null;

            return Settings.TryGetLogType(channel, out var type) ? type.Name : null;
        }

        /// <summary>
        /// A framework channel's name, as a literal per case rather than <c>ToString()</c>, which
        /// allocates a string on every log. The names are the enum's own, because that is what the
        /// settings asset stores for the mandatory channels.
        /// </summary>
        private static string SystemChannelName(SystemLogType systemLogType)
        {
            switch (systemLogType)
            {
                case SystemLogType.All: return "All";
                case SystemLogType.Context: return "Context";
                case SystemLogType.Injection: return "Injection";
                case SystemLogType.Signal: return "Signal";
                case SystemLogType.SignalOperation: return "SignalOperation";
                case SystemLogType.Command: return "Command";
                case SystemLogType.CommandOperation: return "CommandOperation";
                case SystemLogType.Function: return "Function";
                case SystemLogType.Screen: return "Screen";
                case SystemLogType.Pool: return "Pool";
                case SystemLogType.Model: return "Model";
                case SystemLogType.Asset: return "Asset";
                case SystemLogType.Unity: return "Unity";
                case SystemLogType.Compiler: return "Compiler";
                default: return systemLogType.ToString();
            }
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
        /// A line that already knows where it came from: a Connector carrying one module's signal
        /// to another's, which happens inside a callback the wiring left behind. Nothing on the
        /// stack at that moment names the Connector, so the file and line are taken where the
        /// connection is declared and travel with it - and cost nothing, being what the compiler
        /// wrote into the call.
        /// </summary>
        [HideInCallstack]
        [Conditional("ENABLE_LOG")]
        internal static void LogAt(SystemLogType systemLogType, string filePath, int lineNumber, string part1,
            string part2, string part3, string part4 = null)
        {
            if (!Settings.IsLoggingEnabled) return;

            AddLog(systemLogType, part1 + part2 + part3 + part4, LogType.Log, null, false, false,
                filePath, lineNumber);
        }

        /// <summary>
        /// The framework's own bookkeeping: a group initialising, something going back to a pool.
        /// It never works out where it came from, whatever Stack Trace Capture says - these lines
        /// are here to be read in order, and there is nowhere to take a reader that would tell them
        /// anything. The frame it would find is whichever of their own lines happened to be below.
        /// </summary>
        [HideInCallstack]
        [Conditional("ENABLE_LOG")]
        internal static void LogPlumbing(SystemLogType systemLogType, string part1, string part2 = null,
            string part3 = null, string part4 = null)
        {
            if (!Settings.IsLoggingEnabled) return;

            AddLog(systemLogType, part1 + part2 + part3 + part4, LogType.Log, null, false);
        }

        /// <summary>
        /// A signal being dispatched. The game's own signals go on the Signal channel and work out
        /// where they were dispatched from, whatever Stack Trace Capture says, because the line
        /// that dispatched one is the line the reader wants to open. The framework's own go on
        /// SignalOperation and work out nothing - a screen registering itself is not a place
        /// anybody wants to be taken to, and this is the console's most frequent log.
        /// </summary>
        [HideInCallstack]
        [Conditional("ENABLE_LOG")]
        internal static void LogDispatch(bool isFrameworkOwned, string part1, string part2, string part3)
        {
            if (!Settings.IsLoggingEnabled) return;

            if (isFrameworkOwned)
            {
                AddLog(SystemLogType.SignalOperation, part1 + part2 + part3, LogType.Log, null, false);
                return;
            }

            // Dispatched from inside a sequence: the Bind line that declared it is nearer than
            // anything on the stack, and free. Only a dispatch outside one - from a Mediator, or a
            // Context's Launch - has to go looking.
            if (!string.IsNullOrEmpty(_currentDeclarationFile))
            {
                AddLog(SystemLogType.Signal, part1 + part2 + part3, LogType.Log, null, false, false,
                    _currentDeclarationFile, _currentDeclarationLine);
                return;
            }

            AddLog(SystemLogType.Signal, part1 + part2 + part3, LogType.Log, null, true, true);
        }

        /// <summary>
        /// A line about a type: a command executing, a screen opening, a function running. Double
        /// clicking it opens that type's script.
        ///
        /// No stack is captured for one of these. The stack at that moment answers a different
        /// question - a command's execute line is written while the Context that dispatched the
        /// signal is still below it, so the frame found is the binding rather than the Command the
        /// line is about - and the type is the better answer anyway, for nothing.
        /// </summary>
        [HideInCallstack]
        [Conditional("ENABLE_LOG")]
        internal static void LogAbout(SystemLogType systemLogType, Type about, string part1, string part2,
            string part3 = null, string part4 = null)
        {
            if (!Settings.IsLoggingEnabled) return;

            AddLog(systemLogType, part1 + part2 + part3 + part4, LogType.Log, about, false);
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
        public static void Log(string channel, string message)
        {
            AddCustomLog(channel, ResolveMessage(channel, message), LogType.Log);
        }

        [HideInCallstack]
        [Conditional("ENABLE_LOG")]
        public static void Log(string channel, string message, FlowLogProfile profile)
        {
            string formatted = profile != null ? FormatWithProfile(message, profile) : message;
            AddCustomLog(channel, formatted, LogType.Log);
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
        public static void LogWarning(string channel, string message)
        {
            AddCustomLog(channel, ResolveMessage(channel, message), LogType.Warning);
        }

        [HideInCallstack]
        [Conditional("ENABLE_LOG")]
        public static void LogWarning(string channel, string message, FlowLogProfile profile)
        {
            string formatted = profile != null ? FormatWithProfile(message, profile) : message;
            AddCustomLog(channel, formatted, LogType.Warning);
        }

        // ======================== LogError ========================

        [HideInCallstack]
        internal static void LogError(SystemLogType systemLogType, string message, string unityMessage = "",
            UnityEngine.Object context = null)
        {
            WriteError(SystemChannelName(systemLogType), systemLogType, message, unityMessage, context);
        }

        /// <summary>
        /// A framework error that names the type it is about, so the log points at the code
        /// whose author has to change something rather than at the guard clause that caught it.
        /// </summary>
        [HideInCallstack]
        internal static void LogError(SystemLogType systemLogType, string message, Type blame,
            string unityMessage = "", UnityEngine.Object context = null)
        {
            WriteError(SystemChannelName(systemLogType), systemLogType, message, unityMessage, context, blame);
        }

        [HideInCallstack]
        public static void LogError(string channel, string message, UnityEngine.Object context = null)
        {
            WriteError(channel, null, ResolveMessage(channel, message), null, context);
        }

        [HideInCallstack]
        public static void LogError(string channel, string message, FlowLogProfile profile,
            UnityEngine.Object context = null)
        {
            string formatted = profile != null ? FormatWithProfile(message, profile) : message;
            WriteError(channel, null, formatted, null, context);
        }

        /// <summary>
        /// The one path an error takes. It carries no [Conditional] attribute and consults no setting,
        /// because a project that never defined ENABLE_LOG - or turned logging off - is exactly the one
        /// that has to be told something is broken. Every error is written here, and written once.
        /// </summary>
        [HideInCallstack]
        private static void WriteError(string channel, SystemLogType? systemLogType, string message,
            string unityMessage, UnityEngine.Object context, Type blame = null)
        {
#if UNITY_EDITOR
            var log = CreateLogEntry(message, LogType.Error, blame);
            log.Channel = channel;

            if (systemLogType.HasValue)
                log.SystemLogType = systemLogType.Value;

            if (Settings.TryGetLogType(channel, out var typeInfo))
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
        public static void LogLong(string channel, string message, FlowLogProfile profile = null)
        {
            profile ??= Settings.GetResolvedProfile(channel);
            LogLongInternal(channel, message, profile);
        }

        [HideInCallstack]
        private static void LogLongInternal(string channel, string message, FlowLogProfile profile)
        {
            if (!Settings.IsLoggingEnabled) return;

            int messageLength = message.Length;
            if (messageLength <= MaxMessageLength)
            {
                string formatted = profile != null ? FormatWithProfile(message, profile) : message;
                AddCustomLog(channel, formatted, LogType.Log);
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

                    AddCustomLog(channel, prefix + styledChunk + postfix, LogType.Log);
                }
                else
                {
                    AddCustomLog(channel, decorated, LogType.Log);
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
            SystemLogType systemLogType = source == LogSource.Compiler
                ? SystemLogType.Compiler
                : SystemLogType.Unity;

            string channel = SystemChannelName(systemLogType);

            // The channel's tag, the same way the framework's own lines get theirs. This path
            // builds its own entry rather than going through AddLog, so it has to ask as well.
            message = ResolveMessage(channel, message);

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
                Channel = channel,
                SystemLogType = systemLogType,
                StackTrace = stackTrace,
                SourceTrace = stackTrace,
                SourceFilePath = filePath,
                SourceLineNumber = lineNumber,
                Frame = Time.frameCount,
                Realtime = Time.realtimeSinceStartup,
                InPlayMode = Application.isPlaying
            };

            if (Settings.TryGetLogType(channel, out var typeInfo))
                log.LogColor = typeInfo.LogColor;

            // A log from Application.logMessageReceived says where it came from only inside its
            // stack trace, so it is read out here. Without this a Unity message has no source and
            // double-clicking it in the console does nothing.
            if (string.IsNullOrEmpty(log.SourceFilePath) && !string.IsNullOrEmpty(stackTrace))
                FillSourceFromTrace(log, stackTrace);

            AppendLog(log);
#endif
        }

#if UNITY_EDITOR
        private static void FillSourceFromTrace(ConsoleLog log, string stackTrace)
        {
            string[] lines = stackTrace.Split('\n');

            int index = StackFrames.FindFirstGameFrame(lines);

            // The game's frame is the one worth opening, but only if it says where it is. A trace
            // whose first game frame is a Unity callback - or which holds nothing of the game's at
            // all, as a log the package's own editor tooling wrote does - falls back to the first
            // frame that carries a location. Reporting no source for a line Unity's own console
            // opens happily is worse than opening the framework's file.
            if (index < 0 || !StackFrames.TryParseFrame(lines[index], out _, out _))
                index = StackFrames.FindFirstFrameWithLocation(lines);

            if (index < 0) return;

            string frame = lines[index];

            log.SourceClassName = StackFrames.ParseClassName(frame);
            log.SourceTrace = frame;

            if (StackFrames.TryParseFrame(frame, out string filePath, out int lineNumber))
            {
                log.SourceFilePath = filePath;
                log.SourceLineNumber = lineNumber;
            }
        }
#endif

        // ======================== Internal ========================

        [HideInCallstack]
        private static void AddLog(SystemLogType systemLogType, string message, LogType logType, Type blame = null,
            bool captureSource = true, bool forceCapture = false, string filePath = null, int lineNumber = 0)
        {
            if (!Settings.IsLoggingEnabled) return;

            // The channel's profile is what puts the tag on the front - "[Signal]", "[Command]" -
            // so a message says only what happened. Resolved from a cache the settings rebuild
            // whenever a profile changes, and applied here so every one of the framework's own
            // lines gets it rather than only the ones a caller passed a profile to.
            message = ResolveMessage(SystemChannelName(systemLogType), message);

#if UNITY_EDITOR
            var log = CreateLogEntry(message, logType, blame, captureSource, forceCapture);
            log.SystemLogType = systemLogType;

            if (!string.IsNullOrEmpty(filePath))
            {
                log.SourceFilePath = filePath;
                log.SourceLineNumber = lineNumber;
            }

            log.Channel = SystemChannelName(systemLogType);

            if (Settings.TryGetLogType(log.Channel, out var typeInfo))
                log.LogColor = typeInfo.LogColor;

            AppendLog(log);
#endif

            ForwardToUnityConsole(SystemChannelName(systemLogType), message, logType);
        }

        [HideInCallstack]
        private static void AddCustomLog(string channel, string message, LogType logType, Type blame = null)
        {
            if (!Settings.IsLoggingEnabled) return;

#if UNITY_EDITOR
            var log = CreateLogEntry(message, logType, blame);
            log.Channel = channel;

            if (Settings.TryGetLogType(channel, out var typeInfo))
                log.LogColor = typeInfo.LogColor;

            AppendLog(log);
#endif

            ForwardToUnityConsole(channel, message, logType);
        }

#if UNITY_EDITOR
        private static ConsoleLog CreateLogEntry(string message, LogType logType, Type blame = null,
            bool captureSource = true, bool forceCapture = false)
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

            // A log that names the type it is about does not look for a frame. The stack at that
            // moment answers a different question - a command's execute line is written while the
            // Context that dispatched the signal is still on the stack, so the frame found is the
            // binding rather than the Command the line is about.
            if (captureSource && (forceCapture || CapturesSourceFor(logType)))
                GetSourceInfo(log);
            else
                log.SourceTrace = log.StackTrace = NotCaptured;

            log.Frame = Time.frameCount;
            log.Realtime = Time.realtimeSinceStartup;
            log.InPlayMode = Application.isPlaying;
            log.BlameTypeName = blame?.FullName;
            log.FlowId = _currentFlowId;
            log.ParentFlowId = _currentParentFlowId;

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
            log.CollapseKey = CollapseKeys.Build(log.Message, log.StackTrace, log.Channel);

            Logs.Add(log);
            OnLogAdded?.Invoke(log);

            int maxLogCount = Settings.MaxLogCount;
            if (maxLogCount <= 0 || Logs.Count <= maxLogCount + LogTrimChunk)
                return;

            Trimmer.Trim(Logs, maxLogCount);
        }
#endif

        private static void ForwardToUnityConsole(string channel, string message, LogType logType)
        {
            if (!Settings.SendLogsToUnityConsole || !Settings.IsLogTypeVisible(channel)) return;

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
        private static string ResolveMessage(string channel, string message)
        {
            var profile = Settings.GetResolvedProfile(channel);
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

                        // StackFrames rather than System.IO.Path: this path came out of a trace
                        // and can hold characters Path refuses, which it answers with a throw.
                        string shortName = !string.IsNullOrEmpty(log.SourceFilePath)
                            ? StackFrames.FileNameOf(log.SourceFilePath)
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