namespace FlowIoC.ConsoleModule
{
    /// <summary>
    /// The framework's own channels. Every one of them has something that writes to it: a value
    /// here with no producer is a column in the Filters panel that never fills, and a reader who
    /// unticks it learns nothing.
    ///
    /// 35 was Model, retired because nothing could ever write to it. The overloads that take a
    /// SystemLogType are internal, so a game reaches only the string-channel ones - which is the
    /// right shape, since a Model is game code and logs on the module's own channel. A retired
    /// number is never reused.
    /// </summary>
    public enum SystemLogType
    {
        All = 0,
        Context = 5,
        Injection = 10,
        Signal = 11,
        Command = 14,
        CommandOperation = 15,
        Function = 20,
        Screen = 25,
        Pool = 30,
        Asset = 40,

        /// <summary>Anything Unity itself wrote - Debug.Log, an exception, a native warning.</summary>
        Unity = 45,

        /// <summary>A compiler error or warning, taken from CompilationPipeline.</summary>
        Compiler = 50,

        /// <summary>
        /// The framework's own signals - registering a screen, a pool asking for something. Split
        /// off Signal for the reason Command and CommandOperation are split: a reader watching
        /// signals is watching the game's traffic, and every row there should be one they wrote.
        /// </summary>
        SignalOperation = 55
    }
}
