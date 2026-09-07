namespace FlowIoC.ConsoleModule
{
    /// <summary>
    /// Which door a log came in through. Numbered explicitly, like every enum in this project,
    /// because a value inserted in the middle would renumber what is already serialised.
    /// </summary>
    public enum LogSource
    {
        /// <summary>Written by FlowIoC or by the game through FlowLogger.</summary>
        Flow = 0,

        /// <summary>Taken from Application.logMessageReceived - Debug.Log, an exception, a native warning.</summary>
        Unity = 1,

        /// <summary>Taken from CompilationPipeline - a compiler error or warning.</summary>
        Compiler = 2
    }
}
