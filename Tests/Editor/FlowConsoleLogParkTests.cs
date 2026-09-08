using System.Collections.Generic;
using FlowIoC.ConsoleModule;
using FlowIoC.Editor.Console;
using NUnit.Framework;
using UnityEngine;

namespace FlowIoC.Tests
{
    public class FlowConsoleLogParkTests
    {
        private readonly FlowConsoleLogPark _park = new FlowConsoleLogPark();

        private static ConsoleLog Log(string message)
        {
            return new ConsoleLog
            {
                Message = message,
                LogType = LogType.Warning,
                SystemLogType = SystemLogType.Unity,
                LogTypeValue = (int) SystemLogType.Unity,
                Source = LogSource.Unity,
                LogColor = new Color(0.25f, 0.5f, 0.75f, 1f),
                Hour = 9,
                Minute = 41,
                Second = 7,
                Millisecond = 123,
                Frame = 42,
                Realtime = 1.5f,
                FlowId = 3,
                ParentFlowId = 2,
                CollapseKey = 99,
                StackTrace = "Modules.A.B:C () (at Assets/A.cs:1)",
                SourceFilePath = "Assets/A.cs",
                SourceLineNumber = 1,
                SourceClassName = "Modules.A.B",
                BlameTypeName = "Modules.A.B"
            };
        }

        [Test]
        public void A_parked_log_comes_back_whole()
        {
            List<ConsoleLog> restored = _park.Read(_park.Write(new List<ConsoleLog> {Log("saved and read back")}));

            Assert.AreEqual(1, restored.Count);

            ConsoleLog log = restored[0];
            Assert.AreEqual("saved and read back", log.Message);
            Assert.AreEqual(LogType.Warning, log.LogType);
            Assert.AreEqual(SystemLogType.Unity, log.SystemLogType);
            Assert.AreEqual(LogSource.Unity, log.Source);
            Assert.AreEqual(42, log.Frame);
            Assert.AreEqual(3, log.FlowId);
            Assert.AreEqual(2, log.ParentFlowId);
            Assert.AreEqual(99, log.CollapseKey);
            Assert.AreEqual("Assets/A.cs", log.SourceFilePath);
            Assert.AreEqual(1, log.SourceLineNumber);
            Assert.AreEqual("Modules.A.B", log.BlameTypeName);
            Assert.AreEqual(new Color(0.25f, 0.5f, 0.75f, 1f), log.LogColor);
        }

        [Test]
        public void The_order_logs_arrived_in_is_the_order_they_come_back_in()
        {
            var logs = new List<ConsoleLog> {Log("first"), Log("second"), Log("third")};

            List<ConsoleLog> restored = _park.Read(_park.Write(logs));

            Assert.AreEqual(3, restored.Count);
            Assert.AreEqual("first", restored[0].Message);
            Assert.AreEqual("second", restored[1].Message);
            Assert.AreEqual("third", restored[2].Message);
        }

        /// <summary>
        /// A run that logged a hundred thousand lines would put a hundred thousand lines of JSON
        /// through SessionState on every save. The oldest go, because the reason to keep the list
        /// across a compile is to read what just happened.
        /// </summary>
        [Test]
        public void Only_the_newest_logs_are_parked()
        {
            var logs = new List<ConsoleLog>();
            for (int i = 0; i < FlowConsoleLogPark.MaxParkedLogs + 100; i++)
                logs.Add(Log("log " + i));

            List<ConsoleLog> restored = _park.Read(_park.Write(logs));

            Assert.AreEqual(FlowConsoleLogPark.MaxParkedLogs, restored.Count);
            Assert.AreEqual("log 100", restored[0].Message);
        }

        [Test]
        public void Nothing_parked_reads_back_as_nothing()
        {
            Assert.AreEqual(0, _park.Read(null).Count);
            Assert.AreEqual(0, _park.Read("").Count);
            Assert.AreEqual(0, _park.Read(_park.Write(new List<ConsoleLog>())).Count);
            Assert.AreEqual(0, _park.Read(_park.Write(null)).Count);
        }

        /// <summary>
        /// SessionState outlives the code that wrote it. A parked string from an older version of
        /// the package must not throw on the next launch - it reads as nothing.
        /// </summary>
        [Test]
        public void A_parked_string_that_makes_no_sense_reads_back_as_nothing()
        {
            Assert.AreEqual(0, _park.Read("this is not json").Count);
            Assert.AreEqual(0, _park.Read("{\"Logs\":").Count);
        }
    }
}
