using FlowIoC.Editor.Console;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class FlowConsoleEmptyListHintTests
    {
        private readonly FlowConsoleEmptyListHint _hint = new FlowConsoleEmptyListHint();

        /// <summary>
        /// Every channel off is the trap: nothing will ever appear, and an empty list looks exactly
        /// like a quiet game. It is said whether or not any rows exist yet, because the reader who
        /// has not pressed play is the one about to wonder why nothing arrives.
        /// </summary>
        [Test]
        public void Every_channel_off_is_said_even_before_a_row_exists()
        {
            Assert.AreEqual(
                "Every channel is switched off. Switch some on under Filters, or pick Presets > Project defaults.",
                _hint.Text(false, 0));
        }

        [Test]
        public void Rows_held_back_by_the_filters_are_counted()
        {
            Assert.AreEqual("1,234 rows hidden by filters.", _hint.Text(true, 1234));
        }

        [Test]
        public void One_row_reads_as_one_row()
        {
            Assert.AreEqual("1 row hidden by filters.", _hint.Text(true, 1));
        }

        /// <summary>An empty list with nothing to say about it is a quiet game, and gets no hint.</summary>
        [Test]
        public void Nothing_hidden_and_channels_on_says_nothing()
        {
            Assert.IsNull(_hint.Text(true, 0));
        }
    }
}
