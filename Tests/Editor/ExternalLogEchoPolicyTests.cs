using FlowIoC.ConsoleModule;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// With SendLogsToUnityConsole on, a FlowIoC log is handed to Unity, and Unity hands it
    /// straight back through Application.logMessageReceived. Without this the console records
    /// every such log twice.
    /// </summary>
    public class ExternalLogEchoPolicyTests
    {
        private readonly ExternalLogEchoPolicy _policy = new ExternalLogEchoPolicy();

        [Test]
        public void A_message_arriving_while_we_are_writing_to_Unity_is_our_own_echo()
        {
            Assert.IsTrue(_policy.IsEcho(weAreWritingToUnityConsole: true));
        }

        [Test]
        public void A_message_arriving_at_any_other_time_is_somebody_elses()
        {
            Assert.IsFalse(_policy.IsEcho(weAreWritingToUnityConsole: false));
        }
    }
}
