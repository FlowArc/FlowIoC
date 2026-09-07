using System.Collections.Generic;
using FlowIoC.ConsoleModule;
using FlowIoC.Editor.Console;
using NUnit.Framework;
using UnityEngine;

namespace FlowIoC.Tests
{
    public class FlowConsoleExportTests
    {
        private readonly FlowConsoleExport _export = new FlowConsoleExport();

        private static ConsoleLog Log(string message)
        {
            return new ConsoleLog
            {
                Hour = 14, Minute = 22, Second = 7, Millisecond = 91,
                Message = message,
                LogType = LogType.Warning,
                SystemLogType = SystemLogType.Signal,
                StackTrace = "AtHome:Update ()"
            };
        }

        [Test]
        public void A_log_carries_its_time_channel_and_message()
        {
            string text = _export.ToText(new List<ConsoleLog> {Log("hello")}, false);

            StringAssert.Contains("14:22:07.091", text);
            StringAssert.Contains("Signal", text);
            StringAssert.Contains("Warning", text);
            StringAssert.Contains("hello", text);
        }

        [Test]
        public void The_stack_trace_is_left_out_unless_it_is_asked_for()
        {
            Assert.IsFalse(_export.ToText(new List<ConsoleLog> {Log("hello")}, false).Contains("AtHome:Update"));
            Assert.IsTrue(_export.ToText(new List<ConsoleLog> {Log("hello")}, true).Contains("AtHome:Update"));
        }

        /// <summary>
        /// The console colours its own lines with rich text. A text file somebody pastes into a
        /// bug report should carry the sentence, not the markup.
        /// </summary>
        [Test]
        public void Rich_text_tags_are_stripped_so_the_file_reads_as_text()
        {
            string text = _export.ToText(
                new List<ConsoleLog> {Log("<b>bold</b> and <color=#FF0000>red</color>")}, false);

            StringAssert.Contains("bold and red", text);
            StringAssert.DoesNotContain("<b>", text);
            StringAssert.DoesNotContain("<color", text);
        }

        [Test]
        public void Nothing_exports_as_an_empty_string()
        {
            Assert.AreEqual(string.Empty, _export.ToText(new List<ConsoleLog>(), false));
            Assert.AreEqual(string.Empty, _export.ToText(null, false));
        }
    }
}
