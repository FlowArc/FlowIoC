using FlowIoC.ConsoleModule;
using NUnit.Framework;
using UnityEngine;

namespace FlowIoC.Tests
{
    public class FlowConsoleChannelRuleTests
    {
        private readonly FlowConsoleChannelRule _rule = new FlowConsoleChannelRule();

        private static ConsoleLog Log(LogType logType, bool pinned = false)
        {
            return new ConsoleLog {Channel = "Context", LogType = logType, Pinned = pinned};
        }

        [Test]
        public void A_plain_log_answers_to_its_channel_switch()
        {
            Assert.IsTrue(_rule.AnswersToChannels(Log(LogType.Log)));
            Assert.IsTrue(_rule.AnswersToChannels(LogType.Log));
        }

        /// <summary>
        /// The Context channel is off by default and "Shared asset is filed twice!" is written on
        /// it. A switch hides chatter, and a warning is not chatter.
        /// </summary>
        [Test]
        public void A_warning_shows_whatever_its_channel_switch_says()
        {
            Assert.IsFalse(_rule.AnswersToChannels(Log(LogType.Warning)));
            Assert.IsFalse(_rule.AnswersToChannels(LogType.Warning));
        }

        /// <summary>
        /// "Shared asset is not filed on any Root!" is written on the same channel, and a console
        /// that hid it while Unity's showed it was the second console, not the console.
        /// </summary>
        [Test]
        public void An_error_shows_whatever_its_channel_switch_says()
        {
            Assert.IsFalse(_rule.AnswersToChannels(Log(LogType.Error)));
            Assert.IsFalse(_rule.AnswersToChannels(LogType.Error));
        }

        /// <summary>
        /// The kinds Unity's own console folds onto Error - the bridge folds them the same way, but
        /// the rule does not depend on it having done so.
        /// </summary>
        [Test]
        public void An_exception_and_an_assert_show_the_same_way()
        {
            Assert.IsFalse(_rule.AnswersToChannels(LogType.Exception));
            Assert.IsFalse(_rule.AnswersToChannels(LogType.Assert));
        }

        /// <summary>The reader already said this one is not the kind a switch means.</summary>
        [Test]
        public void A_pinned_row_shows_whatever_its_channel_switch_says()
        {
            Assert.IsFalse(_rule.AnswersToChannels(Log(LogType.Log, pinned: true)));
        }

        [Test]
        public void A_missing_row_is_not_a_failure()
        {
            Assert.IsTrue(_rule.AnswersToChannels(null));
        }
    }
}