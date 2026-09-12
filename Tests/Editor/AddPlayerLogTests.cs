using FlowIoC.ConsoleModule;
using NUnit.Framework;
using UnityEngine;

namespace FlowIoC.Tests
{
    public class AddPlayerLogTests
    {
        [SetUp]
        public void Clear()
        {
            FlowLogger.ClearLogs();
        }

        [TearDown]
        public void ClearAgain()
        {
            FlowLogger.ClearLogs();
        }

        private static ConsoleLog DeviceRow(string stackTrace = null)
        {
            return new ConsoleLog
            {
                Channel = "Unity", LogType = LogType.Error, Message = "boom", Player = "Android Pixel 7",
                Source = LogSource.Unity, StackTrace = stackTrace
            };
        }

        [Test]
        public void A_device_row_is_appended_once_with_its_player()
        {
            FlowLogger.AddPlayerLog(DeviceRow());

            Assert.AreEqual(1, FlowLogger.Logs.Count);
            Assert.AreEqual("Android Pixel 7", FlowLogger.Logs[0].Player);
            Assert.AreEqual("boom", FlowLogger.Logs[0].Message);
        }

        /// <summary>
        /// The device sent the trace; the editor works out the file and line from it, the way it
        /// does for a Unity line of its own, so double-clicking the row opens the file.
        /// </summary>
        [Test]
        public void A_row_with_a_trace_and_no_source_gets_its_source_from_the_trace()
        {
            FlowLogger.AddPlayerLog(DeviceRow("Probe.Start () (at Assets/Probe.cs:12)"));

            Assert.AreEqual("Assets/Probe.cs", FlowLogger.Logs[0].SourceFilePath);
            Assert.AreEqual(12, FlowLogger.Logs[0].SourceLineNumber);
        }

        [Test]
        public void A_row_carries_a_collapse_key()
        {
            FlowLogger.AddPlayerLog(DeviceRow());
            FlowLogger.AddPlayerLog(DeviceRow());

            Assert.AreEqual(FlowLogger.Logs[0].CollapseKey, FlowLogger.Logs[1].CollapseKey);
        }

        [Test]
        public void Nothing_is_appended_for_nothing()
        {
            FlowLogger.AddPlayerLog(null);

            Assert.AreEqual(0, FlowLogger.Logs.Count);
        }
    }
}
