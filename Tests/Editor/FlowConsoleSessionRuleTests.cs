using FlowIoC.ConsoleModule;
using FlowIoC.Editor.Console;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class FlowConsoleSessionRuleTests
    {
        private readonly FlowConsoleSessionRule _rule = new FlowConsoleSessionRule();

        private static ConsoleLog Log(bool inPlayMode)
        {
            return new ConsoleLog {InPlayMode = inPlayMode};
        }

        /// <summary>
        /// The line is what the reader looks for when a list holds an editor session and a play
        /// session at once - without it the two run together and the first log of the run looks
        /// like the last log of the edit.
        /// </summary>
        [Test]
        public void The_first_log_of_a_play_session_starts_one()
        {
            Assert.IsTrue(_rule.StartsSession(Log(false), Log(true)));
        }

        [Test]
        public void The_first_log_after_leaving_play_starts_one_too()
        {
            Assert.IsTrue(_rule.StartsSession(Log(true), Log(false)));
        }

        [Test]
        public void Two_logs_from_the_same_side_do_not()
        {
            Assert.IsFalse(_rule.StartsSession(Log(true), Log(true)));
            Assert.IsFalse(_rule.StartsSession(Log(false), Log(false)));
        }

        /// <summary>The top of the list is not a boundary - there is nothing above it to leave.</summary>
        [Test]
        public void The_very_first_row_starts_nothing()
        {
            Assert.IsFalse(_rule.StartsSession(null, Log(true)));
            Assert.IsFalse(_rule.StartsSession(null, Log(false)));
        }

        [Test]
        public void A_row_that_is_not_there_starts_nothing()
        {
            Assert.IsFalse(_rule.StartsSession(Log(true), null));
        }

        [Test]
        public void The_line_says_which_side_it_is_opening()
        {
            Assert.AreEqual("Play mode", _rule.Label(Log(true)));
            Assert.AreEqual("Edit mode", _rule.Label(Log(false)));
        }
    }
}
