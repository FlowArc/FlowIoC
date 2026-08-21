using System.Collections.Generic;
using FlowIoC.BaseModule.Injectable.Utils;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class SignalParamCandidateFinderTests
    {
        private class SubList : List<string> { }

        private SignalParamCandidateFinder _finder;

        [SetUp]
        public void SetUp() => _finder = new SignalParamCandidateFinder();

        [Test]
        public void Find_returns_every_slot_of_the_exact_type_in_payload_order()
        {
            List<int> candidates = _finder.Find(typeof(int), new object[] { "sword", 12, 3 });

            CollectionAssert.AreEqual(new[] { 1, 2 }, candidates);
        }

        [Test]
        public void Find_falls_back_to_assignable_slots_when_nothing_matches_exactly()
        {
            var payload = new object[] { new List<string>(), "text" };

            CollectionAssert.AreEqual(new[] { 0 },
                _finder.Find(typeof(IEnumerable<string>), payload));
        }

        [Test]
        public void Find_prefers_exact_matches_over_assignable_ones()
        {
            var payload = new object[] { new SubList(), new List<string>() };

            CollectionAssert.AreEqual(new[] { 1 }, _finder.Find(typeof(List<string>), payload));
        }

        [Test]
        public void Find_counts_null_as_a_candidate_for_a_reference_type()
        {
            CollectionAssert.AreEqual(new[] { 0, 1 },
                _finder.Find(typeof(string), new object[] { null, "b" }));
        }

        [Test]
        public void Find_does_not_count_null_for_a_non_nullable_value_type()
        {
            CollectionAssert.AreEqual(new[] { 1 },
                _finder.Find(typeof(int), new object[] { null, 5 }));
        }

        [Test]
        public void Find_matches_a_boxed_value_against_a_nullable_property_type()
        {
            CollectionAssert.AreEqual(new[] { 0, 1 },
                _finder.Find(typeof(int?), new object[] { 5, null }));
        }

        [Test]
        public void Find_returns_an_empty_list_for_an_empty_payload()
        {
            Assert.That(_finder.Find(typeof(int), new object[0]), Is.Empty);
        }

        [Test]
        public void Find_does_not_let_a_null_only_exact_pass_suppress_the_assignable_one()
        {
            var payload = new object[] { null, new SubList() };

            CollectionAssert.AreEqual(new[] { 0, 1 },
                _finder.Find(typeof(IEnumerable<string>), payload));
        }
    }
}
