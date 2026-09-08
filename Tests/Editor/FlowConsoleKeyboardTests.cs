using FlowIoC.Editor.Console;
using NUnit.Framework;
using UnityEngine;

namespace FlowIoC.Tests
{
    public class FlowConsoleKeyboardTests
    {
        private readonly FlowConsoleKeyboard _keyboard = new FlowConsoleKeyboard();

        [Test]
        public void Down_takes_the_next_row()
        {
            Assert.IsTrue(_keyboard.TryMove(KeyCode.DownArrow, 3, 10, 5, out int moved));
            Assert.AreEqual(4, moved);
        }

        [Test]
        public void Up_takes_the_row_above()
        {
            Assert.IsTrue(_keyboard.TryMove(KeyCode.UpArrow, 3, 10, 5, out int moved));
            Assert.AreEqual(2, moved);
        }

        [Test]
        public void The_ends_of_the_list_hold()
        {
            Assert.IsTrue(_keyboard.TryMove(KeyCode.UpArrow, 0, 10, 5, out int top));
            Assert.AreEqual(0, top);

            Assert.IsTrue(_keyboard.TryMove(KeyCode.DownArrow, 9, 10, 5, out int bottom));
            Assert.AreEqual(9, bottom);
        }

        /// <summary>
        /// Nothing is selected until something is clicked, and the first key press has to choose
        /// for itself. Down starts at the top of the list and Up starts at the end, so either key
        /// puts a selection on screen from wherever the reader was.
        /// </summary>
        [Test]
        public void With_nothing_selected_Down_starts_at_the_top_and_Up_at_the_end()
        {
            Assert.IsTrue(_keyboard.TryMove(KeyCode.DownArrow, -1, 10, 5, out int down));
            Assert.AreEqual(0, down);

            Assert.IsTrue(_keyboard.TryMove(KeyCode.UpArrow, -1, 10, 5, out int up));
            Assert.AreEqual(9, up);
        }

        [Test]
        public void Home_and_End_take_the_two_ends()
        {
            Assert.IsTrue(_keyboard.TryMove(KeyCode.Home, 4, 10, 5, out int home));
            Assert.AreEqual(0, home);

            Assert.IsTrue(_keyboard.TryMove(KeyCode.End, 4, 10, 5, out int end));
            Assert.AreEqual(9, end);
        }

        [Test]
        public void A_page_is_as_many_rows_as_the_view_holds()
        {
            Assert.IsTrue(_keyboard.TryMove(KeyCode.PageDown, 2, 40, 12, out int down));
            Assert.AreEqual(14, down);

            Assert.IsTrue(_keyboard.TryMove(KeyCode.PageUp, 20, 40, 12, out int up));
            Assert.AreEqual(8, up);
        }

        [Test]
        public void A_page_past_an_end_stops_at_that_end()
        {
            Assert.IsTrue(_keyboard.TryMove(KeyCode.PageDown, 35, 40, 12, out int down));
            Assert.AreEqual(39, down);

            Assert.IsTrue(_keyboard.TryMove(KeyCode.PageUp, 3, 40, 12, out int up));
            Assert.AreEqual(0, up);
        }

        [Test]
        public void A_key_that_moves_nothing_is_left_to_the_window()
        {
            Assert.IsFalse(_keyboard.TryMove(KeyCode.F5, 3, 10, 5, out _));
            Assert.IsFalse(_keyboard.TryMove(KeyCode.Return, 3, 10, 5, out _));
        }

        [Test]
        public void An_empty_list_has_nowhere_to_move()
        {
            Assert.IsFalse(_keyboard.TryMove(KeyCode.DownArrow, -1, 0, 5, out _));
        }

        /// <summary>
        /// Arrowing down at the bottom edge should move the list by one row, not throw the
        /// selected row into the middle of the view - a list that jumps under the reader cannot
        /// be walked. So the reveal is the smallest scroll that puts the row on screen.
        /// </summary>
        [Test]
        public void A_row_below_the_view_is_brought_just_into_it()
        {
            // The view shows 100..500. The row ends at 620, so 120 of it hangs below.
            Assert.AreEqual(220f, _keyboard.Reveal(100f, 400f, 600f, 20f));
        }

        /// <summary>A row whose bottom edge lands exactly on the view's is on screen already.</summary>
        [Test]
        public void A_row_that_ends_flush_with_the_view_moves_nothing()
        {
            Assert.AreEqual(100f, _keyboard.Reveal(100f, 400f, 480f, 20f));
        }

        [Test]
        public void A_row_above_the_view_is_brought_just_into_it()
        {
            Assert.AreEqual(60f, _keyboard.Reveal(100f, 400f, 60f, 20f));
        }

        [Test]
        public void A_row_already_on_screen_moves_nothing()
        {
            Assert.AreEqual(100f, _keyboard.Reveal(100f, 400f, 200f, 20f));
        }

        [Test]
        public void A_reveal_never_scrolls_above_the_top()
        {
            Assert.AreEqual(0f, _keyboard.Reveal(0f, 400f, 0f, 20f));
        }
    }
}