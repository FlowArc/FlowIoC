namespace FlowIoC.ConsoleModule
{
    /// <summary>
    /// How much the logger does, as one developer set it: whether anything is recorded at all,
    /// whether it is mirrored into Unity's console, which logs work out where they came from, how
    /// many are kept, and how much the detail panel shows. In the Editor these live in EditorPrefs
    /// and are edited under Preferences ▸ FlowIoC ▸ Flow Console; they used to be fields of a
    /// committed asset, where one developer raising the capture while chasing a flow turned up in
    /// everybody else's diff.
    ///
    /// A player has nobody at the machine, so it reads the defaults written here: logging is on
    /// wherever it compiled in at all - the Editor and a Development Build - and nothing is
    /// mirrored. Read once and kept, because the logger asks on every line; the setters write
    /// through, so a change is seen at once and survives the next domain reload.
    /// </summary>
    public class FlowConsolePreferences
    {
        private const string Prefix = "FlowIoC.Console.";

        private bool _isLoggingEnabled = true;
        private bool _sendLogsToUnityConsole;
        private bool _deepAnalysis;
        private FlowStackTraceCapture _stackTraceCapture = FlowStackTraceCapture.WarningsAndErrors;
        private int _maxLogCount = 5000;

        public FlowConsolePreferences()
        {
#if UNITY_EDITOR
            _isLoggingEnabled = UnityEditor.EditorPrefs.GetBool(Prefix + nameof(IsLoggingEnabled), _isLoggingEnabled);
            _sendLogsToUnityConsole =
                UnityEditor.EditorPrefs.GetBool(Prefix + nameof(SendLogsToUnityConsole), _sendLogsToUnityConsole);
            _deepAnalysis = UnityEditor.EditorPrefs.GetBool(Prefix + nameof(DeepAnalysis), _deepAnalysis);
            _stackTraceCapture = (FlowStackTraceCapture) UnityEditor.EditorPrefs.GetInt(
                Prefix + nameof(StackTraceCapture), (int) _stackTraceCapture);
            _maxLogCount = UnityEditor.EditorPrefs.GetInt(Prefix + nameof(MaxLogCount), _maxLogCount);
#endif
        }

        /// <summary>The master switch. Off, nothing is recorded and nothing is mirrored; an error still reaches Unity's console.</summary>
        public bool IsLoggingEnabled
        {
            get => _isLoggingEnabled;
            set => Set(ref _isLoggingEnabled, value, nameof(IsLoggingEnabled));
        }

        /// <summary>
        /// Mirror every line into Unity's own console as well, for the two side by side. A plain
        /// log is mirrored only while its channel is on, a warning whatever the switch says, and an
        /// error always - an error is never gated on this.
        /// </summary>
        public bool SendLogsToUnityConsole
        {
            get => _sendLogsToUnityConsole;
            set => Set(ref _sendLogsToUnityConsole, value, nameof(SendLogsToUnityConsole));
        }

        /// <summary>
        /// Whether the detail panel shows the class and the whole stack trace, or only the source
        /// line. Editor-only: a device builds no trace to show.
        /// </summary>
        public bool DeepAnalysis
        {
            get => _deepAnalysis;
            set => Set(ref _deepAnalysis, value, nameof(DeepAnalysis));
        }

        /// <summary>
        /// Which logs work out where they came from. Capturing a source means building the whole
        /// managed stack as a string and picking it apart, and the framework logs every signal,
        /// injection and command - so this is the most expensive thing the console does. Warnings
        /// and errors are the ones somebody follows back, so they are what it is spent on by
        /// default. Raise it to Always while following a flow.
        /// </summary>
        public FlowStackTraceCapture StackTraceCapture
        {
            get => _stackTraceCapture;
            set
            {
                if (_stackTraceCapture == value) return;

                _stackTraceCapture = value;
#if UNITY_EDITOR
                UnityEditor.EditorPrefs.SetInt(Prefix + nameof(StackTraceCapture), (int) value);
#endif
            }
        }

        /// <summary>
        /// How many logs the console keeps. The oldest are dropped past this, so a long play
        /// session does not hold every log it ever wrote. Zero keeps all of them.
        /// </summary>
        public int MaxLogCount
        {
            get => _maxLogCount;
            set
            {
                int clamped = value < 0 ? 0 : value;
                if (_maxLogCount == clamped) return;

                _maxLogCount = clamped;
#if UNITY_EDITOR
                UnityEditor.EditorPrefs.SetInt(Prefix + nameof(MaxLogCount), clamped);
#endif
            }
        }

        private static void Set(ref bool field, bool value, string name)
        {
            if (field == value) return;

            field = value;
#if UNITY_EDITOR
            UnityEditor.EditorPrefs.SetBool(Prefix + name, value);
#endif
        }
    }
}
