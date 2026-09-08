using System.Collections.Generic;
using FlowIoC.Editor.Console;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class FlowConsoleHighlightTests
    {
        private readonly FlowConsoleSearch _search = new FlowConsoleSearch();
        private readonly FlowConsoleHighlight _highlight = new FlowConsoleHighlight();

        private List<HighlightRange> Ranges(string text, string searchText)
        {
            return _highlight.Ranges(text, _search.Parse(searchText));
        }

        [Test]
        public void A_term_is_found_where_it_sits()
        {
            List<HighlightRange> ranges = Ranges("opened MainScreen", "Main");

            Assert.AreEqual(1, ranges.Count);
            Assert.AreEqual(7, ranges[0].Start);
            Assert.AreEqual(4, ranges[0].Length);
        }

        [Test]
        public void Case_does_not_matter_and_every_occurrence_is_found()
        {
            List<HighlightRange> ranges = Ranges("screen SCREEN Screen", "screen");

            Assert.AreEqual(3, ranges.Count);
            Assert.AreEqual(0, ranges[0].Start);
            Assert.AreEqual(7, ranges[1].Start);
            Assert.AreEqual(14, ranges[2].Start);
        }

        [Test]
        public void Two_terms_are_both_marked_and_come_back_in_order()
        {
            List<HighlightRange> ranges = Ranges("opened MainScreen now", "now opened");

            Assert.AreEqual(2, ranges.Count);
            Assert.AreEqual(0, ranges[0].Start);
            Assert.AreEqual(6, ranges[0].Length);
            Assert.AreEqual(18, ranges[1].Start);
        }

        /// <summary>
        /// Two terms that overlap would otherwise paint one rectangle on top of another, and a
        /// translucent highlight painted twice is twice as dark. They become one range.
        /// </summary>
        [Test]
        public void Overlapping_terms_become_one_range()
        {
            List<HighlightRange> ranges = Ranges("MainScreen", "mains ainscreen");

            Assert.AreEqual(1, ranges.Count);
            Assert.AreEqual(0, ranges[0].Start);
            Assert.AreEqual(10, ranges[0].Length);
        }

        [Test]
        public void A_regular_expression_marks_what_it_matches()
        {
            List<HighlightRange> ranges = Ranges("probe 12 and probe 34", "/probe \\d+/");

            Assert.AreEqual(2, ranges.Count);
            Assert.AreEqual(0, ranges[0].Start);
            Assert.AreEqual(8, ranges[0].Length);
            Assert.AreEqual(13, ranges[1].Start);
        }

        /// <summary>An excluded term cannot be in a row that is on screen, so nothing marks it.</summary>
        [Test]
        public void An_excluded_term_marks_nothing()
        {
            Assert.AreEqual(0, Ranges("opened MainScreen", "-opened").Count);
        }

        [Test]
        public void An_empty_search_marks_nothing()
        {
            Assert.AreEqual(0, Ranges("opened MainScreen", "").Count);
            Assert.AreEqual(0, Ranges("opened MainScreen", null).Count);
        }

        [Test]
        public void A_term_that_is_not_there_marks_nothing()
        {
            Assert.AreEqual(0, Ranges("opened MainScreen", "settings").Count);
        }

        [Test]
        public void No_text_at_all_marks_nothing()
        {
            Assert.AreEqual(0, Ranges(null, "screen").Count);
            Assert.AreEqual(0, _highlight.Ranges("screen", null).Count);
        }

        /// <summary>A broken expression matches nothing, so it marks nothing either.</summary>
        [Test]
        public void A_broken_expression_marks_nothing()
        {
            Assert.AreEqual(0, Ranges("opened MainScreen", "/[/").Count);
        }
    }
}
