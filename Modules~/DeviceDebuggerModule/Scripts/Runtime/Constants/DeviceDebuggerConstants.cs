namespace Modules.DeviceDebuggerModule.Constants
{
    public static class DeviceDebuggerConstants
    {
        /// <summary>
        /// The one compile-time fact in the module: the panel exists in the Editor and in a
        /// Development Build, the same line FlowLogger draws for logging. A release player keeps
        /// the Root and a service that answers "not available", and every decision that depends
        /// on this is taken in a Command reading it.
        ///
        /// Static readonly rather than const: a const makes the compiler see one of the two
        /// branches that read it as unreachable, and every game shipping the module would get a
        /// CS0162 warning on each recompile.
        /// </summary>
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static readonly bool IsAvailable = true;
#else
        public static readonly bool IsAvailable = false;
#endif

        // What a game may tune - the ring's size, the trigger's label and gesture window, the stats
        // window and refresh rates - is on CD_DeviceDebugger, not here. What stays is what is not
        // a setting: a compile-time fact, a hierarchy name, a glyph, scanning plumbing.

        /// <summary>The child of the Root that carries the UIDocument and the view.</summary>
        public const string PANEL_OBJECT_NAME = "DeviceDebuggerPanel";

        /// <summary>What a Value row shows before its signal has carried anything.</summary>
        public const string NO_VALUE = "—";

        public const string FRAMEWORK_ASSEMBLY = "FlowIoC";

        /// <summary>Assemblies never scanned for annotated steps: nothing of the game's lives in them.</summary>
        public static readonly string[] SKIPPED_ASSEMBLY_PREFIXES =
        {
            "System", "Unity", "mscorlib", "netstandard", "Mono.", "Microsoft", "nunit", "Newtonsoft", "Bee.", "ReportGeneratorMerged", "JetBrains",
            "Rider", "FlowIoC.Dev."
        };
    }
}