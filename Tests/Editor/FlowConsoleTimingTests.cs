using FlowIoC.Editor.Console;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class FlowConsoleTimingTests
    {
        private readonly FlowConsoleTiming _timing = new FlowConsoleTiming();

        [Test]
        public void The_first_row_has_a_frame_and_nothing_to_measure_from()
        {
            Assert.AreEqual("f120", _timing.Prefix(120, 2f, 0f, false));
        }

        [Test]
        public void A_row_says_how_long_after_the_one_above_it_arrived()
        {
            Assert.AreEqual("f130  +12ms", _timing.Prefix(130, 2.012f, 2f, true));
        }

        [Test]
        public void A_row_in_the_same_millisecond_says_so_rather_than_saying_nothing()
        {
            Assert.AreEqual("f130  +0ms", _timing.Prefix(130, 2f, 2f, true));
        }

        /// <summary>
        /// A gap of seconds read as four digits of milliseconds, which is a number nobody can see
        /// the size of at a glance. Past a second it is written as seconds.
        /// </summary>
        [Test]
        public void A_gap_of_seconds_is_written_in_seconds()
        {
            Assert.AreEqual("f900  +1.20s", _timing.Prefix(900, 3.2f, 2f, true));
            Assert.AreEqual("f900  +12.50s", _timing.Prefix(900, 14.5f, 2f, true));
        }

        [Test]
        public void Just_under_a_second_is_still_milliseconds()
        {
            Assert.AreEqual("f900  +999ms", _timing.Prefix(900, 2.999f, 2f, true));
        }

        /// <summary>
        /// Time starts again when the domain reloads, so a restored log can be later in the list
        /// and earlier on the clock. A negative gap is not a measurement - it reads as none.
        /// </summary>
        [Test]
        public void A_clock_that_went_backwards_reports_no_gap()
        {
            Assert.AreEqual("f5", _timing.Prefix(5, 0.5f, 900f, true));
        }
    }
}
