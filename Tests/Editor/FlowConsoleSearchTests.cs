using FlowIoC.Editor.Console;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class FlowConsoleSearchTests
    {
        private readonly FlowConsoleSearch _search = new FlowConsoleSearch();

        [Test]
        public void An_empty_search_matches_everything()
        {
            Assert.IsTrue(_search.Parse("").Matches("anything"));
            Assert.IsTrue(_search.Parse(null).Matches("anything"));
        }

        [Test]
        public void A_plain_term_matches_case_insensitively()
        {
            FlowConsoleSearchQuery query = _search.Parse("tick");

            Assert.IsTrue(query.Matches("Signal is dispatched: 'Tick'"));
            Assert.IsFalse(query.Matches("Signal is dispatched: 'Tock'"));
        }

        /// <summary>
        /// The reason exclusion exists: one loop drowns everything else, and hiding its whole
        /// channel would hide the lines around it too.
        /// </summary>
        [Test]
        public void A_minus_term_excludes()
        {
            FlowConsoleSearchQuery query = _search.Parse("-tick");

            Assert.IsFalse(query.Matches("Signal is dispatched: 'Tick'"));
            Assert.IsTrue(query.Matches("Signal is dispatched: 'Tock'"));
        }

        [Test]
        public void A_term_and_an_exclusion_are_both_applied()
        {
            FlowConsoleSearchQuery query = _search.Parse("signal -tick");

            Assert.IsTrue(query.Matches("Signal is dispatched: 'Tock'"));
            Assert.IsFalse(query.Matches("Signal is dispatched: 'Tick'"));
            Assert.IsFalse(query.Matches("Command executed"));
        }

        [Test]
        public void Every_plain_term_has_to_match()
        {
            FlowConsoleSearchQuery query = _search.Parse("signal tock");

            Assert.IsTrue(query.Matches("Signal is dispatched: 'Tock'"));
            Assert.IsFalse(query.Matches("Signal is dispatched: 'Tick'"));
        }

        [Test]
        public void A_slash_wrapped_term_is_a_regular_expression()
        {
            FlowConsoleSearchQuery query = _search.Parse("/Tick|Tock/");

            Assert.IsTrue(query.Matches("Tick"));
            Assert.IsTrue(query.Matches("Tock"));
            Assert.IsFalse(query.Matches("Tack"));
        }

        /// <summary>
        /// A search field is typed into one character at a time, so it spends most of its life
        /// holding half an expression. A broken pattern matches nothing rather than throwing on
        /// every repaint.
        /// </summary>
        [Test]
        public void A_broken_pattern_matches_nothing_and_does_not_throw()
        {
            FlowConsoleSearchQuery query = null;

            Assert.DoesNotThrow(() => query = _search.Parse("/[unclosed/"));
            Assert.DoesNotThrow(() => query.Matches("anything"));
            Assert.IsFalse(query.Matches("anything"));
        }

        [Test]
        public void A_null_message_never_matches_a_term()
        {
            Assert.IsFalse(_search.Parse("tick").Matches(null));
            Assert.IsTrue(_search.Parse("").Matches(null));
        }
    }
}
