namespace FlowIoC.ConsoleModule
{
    /// <summary>
    /// FlowIoC hands its own lines to Unity - a warning and an error always, a plain log while
    /// SendLogsToUnityConsole is on - and Unity hands each back through
    /// Application.logMessageReceived. The bridge asks this before recording, so a line that made
    /// that round trip is written once rather than twice.
    /// </summary>
    public class ExternalLogEchoPolicy
    {
        public bool IsEcho(bool weAreWritingToUnityConsole) => weAreWritingToUnityConsole;
    }
}
