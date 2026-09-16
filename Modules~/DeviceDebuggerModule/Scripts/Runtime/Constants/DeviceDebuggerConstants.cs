namespace Modules.DeviceDebuggerModule.Constants
{
    public static class DeviceDebuggerConstants
    {
        /// <summary>
        /// The one compile-time fact in the module: the panel exists in the Editor and in a
        /// Development Build, the same line FlowLogger draws for logging. A release player keeps
        /// the Root and a service that answers "not available", and every decision that depends
        /// on this is taken in a Command reading it.
        /// </summary>
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public const bool IS_AVAILABLE = true;
#else
        public const bool IS_AVAILABLE = false;
#endif

        public const int DEFAULT_LOG_CAPACITY = 1000;

        /// <summary>Room under the panel for a phone's rounded corners and gesture bar, in dp.</summary>
        public const int DEFAULT_BOTTOM_INSET = 24;

        /// <summary>Frame times the stats sampler keeps; two seconds at sixty.</summary>
        public const int STATS_WINDOW = 120;

        public const float TRIPLE_TAP_WINDOW_SECONDS = 1f;

        /// <summary>The child of the Root that carries the UIDocument and the view.</summary>
        public const string PANEL_OBJECT_NAME = "DeviceDebuggerPanel";

        public const string TRIGGER_LABEL = "FlowIoC";

        /// <summary>What a Value row shows before its signal has carried anything.</summary>
        public const string NO_VALUE = "—";

        public const string FRAMEWORK_ASSEMBLY = "FlowIoC";

        /// <summary>Assemblies never scanned for annotated steps: nothing of the game's lives in them.</summary>
        public static readonly string[] SKIPPED_ASSEMBLY_PREFIXES =
        {
            "System", "Unity", "mscorlib", "netstandard", "Mono.", "Microsoft", "nunit", "Newtonsoft", "Bee.", "ReportGeneratorMerged", "JetBrains", "Rider", "FlowIoC.Dev."
        };
    }
}
