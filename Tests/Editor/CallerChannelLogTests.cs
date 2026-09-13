using System.Runtime.CompilerServices;
using FlowIoC.ConsoleModule;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FlowIoC.Tests
{
    /// <summary>
    /// A log that names no channel goes on the caller's module. This file sits under
    /// Packages/FlowIoC/Tests/Editor, which is no module, so its rows land on Default - and the
    /// row carries this file and the line the call is on without a stack having been built.
    /// </summary>
    public class CallerChannelLogTests
    {
        private bool _wasLoggingEnabled;
        private FlowStackTraceCapture _wasCapture;

        [SetUp]
        public void Remember()
        {
            _wasLoggingEnabled = FlowLogger.Preferences.IsLoggingEnabled;
            _wasCapture = FlowLogger.Preferences.StackTraceCapture;
            FlowLogger.Preferences.IsLoggingEnabled = true;
            FlowLogger.ClearLogs();
        }

        [TearDown]
        public void Restore()
        {
            FlowLogger.Preferences.IsLoggingEnabled = _wasLoggingEnabled;
            FlowLogger.Preferences.StackTraceCapture = _wasCapture;
            FlowLogger.ClearLogs();
        }

        private static ConsoleLog Last() => FlowLogger.Logs[FlowLogger.Logs.Count - 1];

        private static string ThisFile([CallerFilePath] string file = "") => file;

        private static int ThisLine([CallerLineNumber] int line = 0) => line;

        [Test]
        public void A_log_with_no_channel_goes_on_the_callers_module_and_knows_its_line()
        {
            FlowLogger.Preferences.StackTraceCapture = FlowStackTraceCapture.Never;

            FlowLogger.Log("caller-channel-probe");
            int line = ThisLine() - 1;

            ConsoleLog log = Last();

            Assert.AreEqual(FlowModule.Default, log.Channel);
            Assert.AreEqual(ThisFile(), log.SourceFilePath);
            Assert.AreEqual(line, log.SourceLineNumber);
            StringAssert.Contains("caller-channel-probe", log.Message);
        }

        [Test]
        public void A_warning_with_no_channel_goes_the_same_way()
        {
            FlowLogger.LogWarning("caller-channel-warning-probe");

            ConsoleLog log = Last();

            Assert.AreEqual(LogType.Warning, log.LogType);
            Assert.AreEqual(FlowModule.Default, log.Channel);
            Assert.AreEqual(ThisFile(), log.SourceFilePath);
        }

        /// <summary>
        /// An error keeps its stack, and the caller's file stands in only when the capture found
        /// nothing - here the capture is off, so the file is the caller's.
        /// </summary>
        [Test]
        public void An_error_with_no_channel_goes_on_the_callers_module()
        {
            FlowLogger.Preferences.StackTraceCapture = FlowStackTraceCapture.Never;
            LogAssert.Expect(LogType.Error, "caller-channel-error-probe");

            FlowLogger.LogError("caller-channel-error-probe");

            ConsoleLog log = Last();

            Assert.AreEqual(LogType.Error, log.LogType);
            Assert.AreEqual(FlowModule.Default, log.Channel);
            Assert.AreEqual(ThisFile(), log.SourceFilePath);
        }

        /// <summary>The two-string call is still channel then message: the compiler never mistakes it for a message and a line.</summary>
        [Test]
        public void A_channel_named_by_hand_is_still_the_channel()
        {
            FlowLogger.Log("Signal", "named-channel-probe");

            Assert.AreEqual("Signal", Last().Channel);
        }
    }
}
