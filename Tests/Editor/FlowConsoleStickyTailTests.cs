using FlowIoC.Editor.Console;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class FlowConsoleStickyTailTests
    {
        private readonly FlowConsoleStickyTail _tail = new FlowConsoleStickyTail();

        [Test]
        public void The_bottom_of_content_that_fits_is_the_top()
        {
            Assert.AreEqual(0f, _tail.BottomOf(400f, 200f));
        }

        [Test]
        public void The_bottom_of_content_taller_than_the_view_is_what_hangs_below_it()
        {
            Assert.AreEqual(600f, _tail.BottomOf(400f, 1000f));
        }

        [Test]
        public void A_view_scrolled_to_the_bottom_is_at_the_bottom()
        {
            Assert.IsTrue(_tail.IsAtBottom(600f, 400f, 1000f));
        }

        [Test]
        public void A_view_scrolled_up_is_not_at_the_bottom()
        {
            Assert.IsFalse(_tail.IsAtBottom(120f, 400f, 1000f));
        }

        /// <summary>
        /// A scroll position is a float that a drag leaves fractionally short of the end. Without
        /// a tolerance the tail would come off the moment the reader let go of the scrollbar.
        /// </summary>
        [Test]
        public void A_pixel_short_of_the_end_still_counts_as_the_bottom()
        {
            Assert.IsTrue(_tail.IsAtBottom(599f, 400f, 1000f));
        }

        [Test]
        public void Content_shorter_than_the_view_is_always_at_the_bottom()
        {
            Assert.IsTrue(_tail.IsAtBottom(0f, 400f, 200f));
        }

        /// <summary>The behaviour this exists for: new logs push the view down when it was resting
        /// at the end, so a running game reads like a tail.</summary>
        [Test]
        public void A_view_that_was_at_the_bottom_follows_new_content_down()
        {
            Assert.AreEqual(900f, _tail.Follow(true, 600f, 400f, 1300f));
        }

        /// <summary>And the other half of it: a reader who scrolled up to read something keeps
        /// their place while logs keep arriving.</summary>
        [Test]
        public void A_reader_who_scrolled_up_is_left_where_they_were()
        {
            Assert.AreEqual(120f, _tail.Follow(false, 120f, 400f, 1300f));
        }

        /// <summary>
        /// Content shrinks when a filter is switched on or Collapse folds the list. The window
        /// used to answer that by jumping to the top, which loses the reader's place for no
        /// reason - the end of the shorter list is the nearest position that still exists.
        /// </summary>
        [Test]
        public void A_position_past_a_shrunken_end_is_pulled_back_to_the_end_not_to_the_top()
        {
            Assert.AreEqual(200f, _tail.Follow(false, 600f, 400f, 600f));
        }

        [Test]
        public void A_list_that_shrank_to_less_than_a_screen_goes_to_the_top()
        {
            Assert.AreEqual(0f, _tail.Follow(false, 600f, 400f, 100f));
        }
    }
}
