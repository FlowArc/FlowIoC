using FlowIoC.ConsoleModule;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FlowIoC.Tests
{
    public class AddExternalLogTests
    {
        /// <summary>The Default project channel, which CD_FlowConsole always creates.</summary>
        private const string DEFAULT_CHANNEL = "Default";

        private bool _wasLoggingEnabled;
        private bool _wasForwarding;

        [SetUp]
        public void Remember()
        {
            _wasLoggingEnabled = FlowLogger.Settings.IsLoggingEnabled;
            _wasForwarding = FlowLogger.Settings.SendLogsToUnityConsole;
            FlowLogger.ClearLogs();
        }

        /// <summary>
        /// The setting is put back as it was found rather than forced on. CD_FlowConsole is a
        /// committed asset, and a test that leaves it switched differently would turn up in
        /// somebody's diff.
        /// </summary>
        [TearDown]
        public void Restore()
        {
            FlowLogger.Settings.IsLoggingEnabled = _wasLoggingEnabled;
            FlowLogger.Settings.SendLogsToUnityConsole = _wasForwarding;
            FlowLogger.ClearLogs();
        }

        private static int RowsMentioning(string text)
        {
            int rows = 0;

            foreach (ConsoleLog log in FlowLogger.Logs)
            {
                if (log.Message != null && log.Message.Contains(text))
                    rows++;
            }

            return rows;
        }

        /// <summary>
        /// With SendLogsToUnityConsole on, the log FlowIoC hands to Unity comes straight back
        /// through the editor bridge's Application.logMessageReceived hook. Without the
        /// re-entrancy flag the console would show every such log twice.
        /// </summary>
        [Test]
        public void A_log_mirrored_into_Unitys_console_is_recorded_once()
        {
            FlowLogger.Settings.IsLoggingEnabled = true;
            FlowLogger.Settings.SendLogsToUnityConsole = true;
            FlowLogger.ClearLogs();

            FlowLogger.Log(DEFAULT_CHANNEL, "mirrored-log-probe");

            Assert.AreEqual(1, RowsMentioning("mirrored-log-probe"));
        }

        /// <summary>
        /// An error reaches Unity's console whether or not SendLogsToUnityConsole is on, so it
        /// takes the same round trip and needs the same guard.
        /// </summary>
        [Test]
        public void An_error_mirrored_into_Unitys_console_is_recorded_once()
        {
            LogAssert.Expect(LogType.Error, "mirrored-error-probe");

            FlowLogger.Settings.SendLogsToUnityConsole = false;
            FlowLogger.ClearLogs();

            FlowLogger.LogError(DEFAULT_CHANNEL, "mirrored-error-probe");

            Assert.AreEqual(1, RowsMentioning("mirrored-error-probe"));
        }

        [Test]
        public void A_Unity_message_is_recorded_on_the_Unity_channel()
        {
            FlowLogger.AddExternalLog(LogSource.Unity, LogType.Log, "hello", "AtHome:Start ()", null, 0);

            Assert.AreEqual(1, FlowLogger.Logs.Count);
            Assert.AreEqual(LogSource.Unity, FlowLogger.Logs[0].Source);
            Assert.AreEqual("Unity", FlowLogger.Logs[0].Channel);

            // The channel's profile puts its tag on the front, so the message is what it ends with
            // rather than the whole of it.
            StringAssert.EndsWith("hello", FlowLogger.Logs[0].Message);
        }

        [Test]
        public void A_compiler_message_is_recorded_on_the_Compiler_channel()
        {
            FlowLogger.AddExternalLog(LogSource.Compiler, LogType.Error, "CS0103", null, "Assets/A.cs", 12);

            Assert.AreEqual("Compiler", FlowLogger.Logs[0].Channel);
            Assert.AreEqual("Assets/A.cs", FlowLogger.Logs[0].SourceFilePath);
            Assert.AreEqual(12, FlowLogger.Logs[0].SourceLineNumber);
        }

        /// <summary>
        /// The whole reason this door exists. A developer who turned logging off still has to
        /// be able to read Unity's own console in this window.
        /// </summary>
        [Test]
        public void A_Unity_message_is_recorded_even_with_logging_switched_off()
        {
            FlowLogger.Settings.IsLoggingEnabled = false;

            FlowLogger.AddExternalLog(LogSource.Unity, LogType.Error, "boom", null, null, 0);

            Assert.AreEqual(1, FlowLogger.Logs.Count);
        }

        [Test]
        public void An_external_log_carries_a_collapse_key()
        {
            FlowLogger.AddExternalLog(LogSource.Unity, LogType.Log, "same", "AtHome:Start ()", null, 0);
            FlowLogger.AddExternalLog(LogSource.Unity, LogType.Log, "same", "AtHome:Start ()", null, 0);

            Assert.AreEqual(FlowLogger.Logs[0].CollapseKey, FlowLogger.Logs[1].CollapseKey);
        }

        [Test]
        public void A_flow_log_is_marked_as_coming_from_the_framework()
        {
            // Every error also reaches Unity's console, on purpose, and the test runner treats
            // an unexpected one as a failure.
            LogAssert.Expect(LogType.Error, "an error");

            FlowLogger.LogError(DEFAULT_CHANNEL, "an error");

            Assert.AreEqual(LogSource.Flow, FlowLogger.Logs[0].Source);
        }

        /// <summary>
        /// A channel's switch holds back its chatter and nothing else, in Unity's console as in
        /// the window. The developer's switch on the Default channel is thrown for the test and
        /// put back as it was found, because it is theirs.
        /// </summary>
        [Test]
        public void A_warning_on_a_hidden_channel_is_still_mirrored_and_a_log_is_not()
        {
            Assert.IsTrue(FlowLogger.Settings.TryGetLogType(DEFAULT_CHANNEL, out var channel));

            bool wasShown = FlowLogger.Settings.Visibility.IsShown(channel);
            var mirrored = new System.Collections.Generic.List<string>();

            void Capture(string condition, string stackTrace, LogType type) => mirrored.Add(type + ":" + condition);

            FlowLogger.Settings.IsLoggingEnabled = true;
            FlowLogger.Settings.SendLogsToUnityConsole = true;
            FlowLogger.Settings.Visibility.Show(channel, false);
            Application.logMessageReceived += Capture;

            try
            {
                FlowLogger.LogWarning(DEFAULT_CHANNEL, "hidden-channel-warning-probe");
                FlowLogger.Log(DEFAULT_CHANNEL, "hidden-channel-log-probe");
            }
            finally
            {
                Application.logMessageReceived -= Capture;
                FlowLogger.Settings.Visibility.Show(channel, wasShown);
            }

            Assert.IsTrue(mirrored.Exists(line => line.StartsWith("Warning:") && line.Contains("hidden-channel-warning-probe")));
            Assert.IsFalse(mirrored.Exists(line => line.Contains("hidden-channel-log-probe")));
        }
    }
}