namespace FlowIoC.ConsoleModule
{
    /// <summary>
    /// How much of a log's origin the console works out as the log is written. Capturing it means
    /// building the whole managed stack as a string and picking it apart, which the framework would
    /// otherwise pay for on every signal, injection and command - so the default spends it only
    /// where somebody is going to follow it back.
    /// </summary>
    public enum FlowStackTraceCapture
    {
        /// <summary>Nothing is captured. The cheapest, and the console shows no source for any log.</summary>
        Never = 0,

        /// <summary>Warnings and errors carry their source; ordinary logs do not. The default.</summary>
        WarningsAndErrors = 1,

        /// <summary>Every log carries its source. What to turn on while following a flow.</summary>
        Always = 2
    }
}
