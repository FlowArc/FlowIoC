using System.Collections.Generic;
using FlowIoC.ConsoleModule;
using FlowIoC.Editor.Console;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class FlowConsolePinsTests
    {
        private readonly FlowConsolePins _pins = new FlowConsolePins();

        private static List<ConsoleLog> Logs(params bool[] pinned)
        {
            var logs = new List<ConsoleLog>();

            for (int i = 0; i < pinned.Length; i++)
                logs.Add(new ConsoleLog {Message = "log " + i, Pinned = pinned[i]});

            return logs;
        }

        [Test]
        public void Pinning_a_log_marks_it_and_pinning_it_again_lets_it_go()
        {
            var log = new ConsoleLog();

            _pins.Toggle(log);
            Assert.IsTrue(log.Pinned);

            _pins.Toggle(log);
            Assert.IsFalse(log.Pinned);
        }

        [Test]
        public void A_list_under_the_limit_is_left_alone()
        {
            List<ConsoleLog> logs = Logs(false, false, false);

            _pins.Trim(logs, 10);

            Assert.AreEqual(3, logs.Count);
        }

        [Test]
        public void Trimming_drops_the_oldest_first()
        {
            List<ConsoleLog> logs = Logs(false, false, false, false, false);

            _pins.Trim(logs, 2);

            Assert.AreEqual(2, logs.Count);
            Assert.AreEqual("log 3", logs[0].Message);
            Assert.AreEqual("log 4", logs[1].Message);
        }

        /// <summary>
        /// The whole point of pinning is that a log stays while the rest scroll away, so the trim
        /// that keeps the list bounded is the first thing that must respect it.
        /// </summary>
        [Test]
        public void A_pinned_log_survives_a_trim_that_takes_its_neighbours()
        {
            List<ConsoleLog> logs = Logs(true, false, false, false, false);

            _pins.Trim(logs, 2);

            Assert.AreEqual(2, logs.Count);
            Assert.AreEqual("log 0", logs[0].Message);
            Assert.AreEqual("log 4", logs[1].Message);
        }

        /// <summary>
        /// A reader who pins more than the limit gets what they asked for. The list going over is
        /// better than throwing away something they said to keep.
        /// </summary>
        [Test]
        public void More_pins_than_the_limit_keeps_them_all()
        {
            List<ConsoleLog> logs = Logs(true, true, true, false);

            _pins.Trim(logs, 2);

            Assert.AreEqual(3, logs.Count);
            Assert.AreEqual("log 0", logs[0].Message);
            Assert.AreEqual("log 2", logs[2].Message);
        }

        [Test]
        public void No_limit_at_all_trims_nothing()
        {
            List<ConsoleLog> logs = Logs(false, false, false);

            _pins.Trim(logs, 0);
            Assert.AreEqual(3, logs.Count);

            _pins.Trim(logs, -1);
            Assert.AreEqual(3, logs.Count);
        }

        [Test]
        public void Nothing_to_trim_is_not_a_failure()
        {
            _pins.Trim(null, 5);
            _pins.Toggle(null);
        }

        /// <summary>
        /// The automatic clears - entering play mode, recompiling, starting a build - clear to get
        /// the last run's noise out of the way, and a pinned log is the reader saying this one is
        /// not noise.
        /// </summary>
        [Test]
        public void An_automatic_clear_empties_the_console_but_leaves_the_pins()
        {
            FlowLogger.ClearLogs();
            FlowLogger.Logs.AddRange(Logs(false, true, false));

            FlowLogger.ClearLogsKeepingPinned();

            Assert.AreEqual(1, FlowLogger.Logs.Count);
            Assert.AreEqual("log 1", FlowLogger.Logs[0].Message);

            FlowLogger.ClearLogs();
        }

        /// <summary>Pressing Clear is somebody asking for exactly that, pins included.</summary>
        [Test]
        public void The_Clear_button_empties_everything()
        {
            FlowLogger.ClearLogs();
            FlowLogger.Logs.AddRange(Logs(true, true));

            FlowLogger.ClearLogs();

            Assert.AreEqual(0, FlowLogger.Logs.Count);
        }
    }
}