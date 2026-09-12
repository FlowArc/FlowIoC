using FlowIoC.ConsoleModule;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FlowIoC.Tests
{
    /// <summary>
    /// A module's channel follows the module's name and keeps everything else: its number, which
    /// is what every log line already written carries, and the colour and default somebody chose.
    /// </summary>
    public class RenameLogTypeTests
    {
        private CD_FlowConsole _settings;

        [SetUp]
        public void Fresh() => _settings = ScriptableObject.CreateInstance<CD_FlowConsole>();

        [TearDown]
        public void Gone() => Object.DestroyImmediate(_settings);

        [Test]
        public void The_channel_keeps_its_number_colour_and_default_under_the_new_name()
        {
            CD_FlowConsole.FlowConsoleLogTypeCVO added = _settings.AddLogType("CounterModule", 77, Color.red);
            added.IsVisibleByDefault = false;
            added.IsAutoRegistered = true;

            Assert.IsTrue(_settings.RenameLogType("CounterModule", "TimerModule"));

            Assert.IsFalse(_settings.TryGetLogType("CounterModule", out _));
            Assert.IsTrue(_settings.TryGetLogType("TimerModule", out CD_FlowConsole.FlowConsoleLogTypeCVO renamed));
            Assert.AreEqual(77, renamed.Value);
            Assert.AreEqual(Color.red, renamed.LogColor);
            Assert.IsFalse(renamed.IsVisibleByDefault);
            Assert.IsTrue(renamed.IsAutoRegistered);
            Assert.IsTrue(_settings.TryGetLogType(77, out CD_FlowConsole.FlowConsoleLogTypeCVO byValue));
            Assert.AreEqual("TimerModule", byValue.Name);
        }

        [Test]
        public void A_mandatory_channel_is_refused()
        {
            CD_FlowConsole.FlowConsoleLogTypeCVO mandatory = _settings.LogTypes.Find(logType => logType.IsMandatory);
            LogAssert.Expect(LogType.Warning, "Cannot rename mandatory log type: " + mandatory.Name);

            Assert.IsFalse(_settings.RenameLogType(mandatory.Name, "Renamed"));
            Assert.IsTrue(_settings.TryGetLogType(mandatory.Name, out _));
        }

        [Test]
        public void A_name_another_channel_has_is_refused()
        {
            _settings.AddLogType("CounterModule");
            _settings.AddLogType("TimerModule");
            LogAssert.Expect(LogType.Warning, "Log type 'TimerModule' already exists.");

            Assert.IsFalse(_settings.RenameLogType("CounterModule", "TimerModule"));
            Assert.IsTrue(_settings.TryGetLogType("CounterModule", out _));
        }

        [Test]
        public void An_unknown_channel_answers_false_and_says_nothing()
        {
            Assert.IsFalse(_settings.RenameLogType("Nope", "Yes"));
        }

        [Test]
        public void A_change_of_case_only_is_a_rename_of_the_same_channel()
        {
            // 123 rather than a low number: the system channels own 0 to 60 and Default owns 100,
            // and AddLogType hands out a different number when the one asked for is taken.
            _settings.AddLogType("countermodule", 123);

            Assert.IsTrue(_settings.RenameLogType("countermodule", "CounterModule"));
            Assert.IsTrue(_settings.TryGetLogType(123, out CD_FlowConsole.FlowConsoleLogTypeCVO renamed));
            Assert.AreEqual("CounterModule", renamed.Name);
        }
    }
}
