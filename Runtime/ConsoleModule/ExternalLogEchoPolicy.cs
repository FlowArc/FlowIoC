namespace FlowIoC.ConsoleModule
{
    /// <summary>
    /// With SendLogsToUnityConsole on, FlowIoC hands its own log to Unity and Unity hands it
    /// back through Application.logMessageReceived. The bridge asks this before recording, so
    /// a log that made that round trip is written once rather than twice.
    /// </summary>
    public class ExternalLogEchoPolicy
    {
        public bool IsEcho(bool weAreWritingToUnityConsole) => weAreWritingToUnityConsole;
    }
}
