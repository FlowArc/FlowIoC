using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.Injectable.Utils;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class SignalParamResolverTests
    {
        private SignalParamResolver _resolver;
        private SignalParamEntryBuilder _builder;

        [SetUp]
        public void SetUp()
        {
            _resolver = new SignalParamResolver();
            _builder = new SignalParamEntryBuilder();
        }

        private void Resolve(object target, params object[] values)
            => _resolver.Resolve(target, _builder.Build(target.GetType()), values);

        private class IndexedInts
        {
            [SignalParam(0)] private int _x { get; set; }
            [SignalParam(1)] private int _y { get; set; }
            public int X => _x;
            public int Y => _y;
        }

        private class UnindexedInts
        {
            [SignalParam] private int _x { get; set; }
            [SignalParam] private int _y { get; set; }
            public int X => _x;
            public int Y => _y;
        }

        private class MixedPayload
        {
            [SignalParam] private string _weapon { get; set; }
            [SignalParam(0)] private int _amount { get; set; }
            [SignalParam(1)] private int _crit { get; set; }
            public string Weapon => _weapon;
            public int Amount => _amount;
            public int Crit => _crit;
        }

        private class ExplicitThenImplicit
        {
            [SignalParam(0)] private int _first { get; set; }
            [SignalParam] private int _next { get; set; }
            public int First => _first;
            public int Next => _next;
        }

        private class TwoStrings
        {
            [SignalParam(0)] private string _from { get; set; }
            [SignalParam(1)] private string _to { get; set; }
            public string From => _from;
            public string To => _to;
        }

        private class IndexTooHigh
        {
            [SignalParam(3)] private int _crit { get; set; }
        }

        private class SameSlotTwice
        {
            [SignalParam(0)] private int _a { get; set; }
            [SignalParam(0)] private int _b { get; set; }
            public int A => _a;
            public int B => _b;
        }

        private class WantsContext
        {
            [SignalParam] private IContext _context { get; set; }
            public IContext Context => _context;
        }

        private class ImplicitThenExplicit
        {
            [SignalParam] private int _next { get; set; }
            [SignalParam(0)] private int _first { get; set; }
            public int Next => _next;
            public int First => _first;
        }

        private class NegativeIndex
        {
            [SignalParam(-1)] private int _bad { get; set; }
        }

        private class WeaponAndSkin
        {
            [SignalParam] private string _weapon { get; set; }
            [SignalParam] private string _skin { get; set; }
            public string Weapon => _weapon;
            public string Skin => _skin;
        }

        [Test]
        public void Indexed_properties_of_one_type_take_distinct_slots()
        {
            var target = new IndexedInts();
            Resolve(target, 3, 7);

            Assert.That(target.X, Is.EqualTo(3));
            Assert.That(target.Y, Is.EqualTo(7));
            Assert.That(_resolver.Diagnostics, Is.Empty);
        }

        [Test]
        public void Unindexed_properties_consume_the_payload_in_declaration_order()
        {
            var target = new UnindexedInts();
            Resolve(target, 3, 7);

            Assert.That(target.X, Is.EqualTo(3));
            Assert.That(target.Y, Is.EqualTo(7));
        }

        [Test]
        public void An_index_counts_within_its_own_type_not_across_the_payload()
        {
            var target = new MixedPayload();
            Resolve(target, "sword", 12, 3);

            Assert.That(target.Weapon, Is.EqualTo("sword"));
            Assert.That(target.Amount, Is.EqualTo(12));
            Assert.That(target.Crit, Is.EqualTo(3));
            Assert.That(_resolver.Diagnostics, Is.Empty);
        }

        [Test]
        public void An_unindexed_property_skips_a_slot_an_indexed_one_claimed()
        {
            var target = new ExplicitThenImplicit();
            Resolve(target, 3, 7);

            Assert.That(target.First, Is.EqualTo(3));
            Assert.That(target.Next, Is.EqualTo(7));
        }

        [Test]
        public void A_dispatched_null_binds_without_a_diagnostic()
        {
            var target = new TwoStrings();
            Resolve(target, null, "b");

            Assert.That(target.From, Is.Null);
            Assert.That(target.To, Is.EqualTo("b"));
            Assert.That(_resolver.Diagnostics, Is.Empty);
        }

        [Test]
        public void An_index_beyond_the_candidate_count_reports_IndexOutOfRange()
        {
            Resolve(new IndexTooHigh(), "sword", 12);

            Assert.That(_resolver.Diagnostics.Count, Is.EqualTo(1));
            Assert.That(_resolver.Diagnostics[0].Kind,
                Is.EqualTo(SignalParamDiagnosticKind.IndexOutOfRange));
            Assert.That(_resolver.Diagnostics[0].CandidateCount, Is.EqualTo(1));
            Assert.That(_resolver.Diagnostics[0].RequestedIndex, Is.EqualTo(3));
        }

        [Test]
        public void Two_properties_claiming_one_slot_report_DuplicateClaim()
        {
            var target = new SameSlotTwice();
            Resolve(target, 3, 7);

            Assert.That(target.A, Is.EqualTo(3));
            Assert.That(target.B, Is.EqualTo(0));
            Assert.That(_resolver.Diagnostics.Count, Is.EqualTo(1));
            Assert.That(_resolver.Diagnostics[0].Kind,
                Is.EqualTo(SignalParamDiagnosticKind.DuplicateClaim));
            Assert.That(_resolver.Diagnostics[0].PropertyName, Is.EqualTo("_b"));
            Assert.That(_resolver.Diagnostics[0].ClaimingPropertyName, Is.EqualTo("_a"));
        }

        [Test]
        public void An_unindexed_property_with_no_free_slot_reports_NoFreeSlot()
        {
            var target = new UnindexedInts();
            Resolve(target, 3);

            Assert.That(target.X, Is.EqualTo(3));
            Assert.That(_resolver.Diagnostics.Count, Is.EqualTo(1));
            Assert.That(_resolver.Diagnostics[0].Kind,
                Is.EqualTo(SignalParamDiagnosticKind.NoFreeSlot));
            Assert.That(_resolver.Diagnostics[0].PropertyName, Is.EqualTo("_y"));
            Assert.That(_resolver.Diagnostics[0].ClaimedCount, Is.EqualTo(1));
        }

        [Test]
        public void An_interface_typed_property_binds_through_the_assignable_pass()
        {
            var context = new Context();
            var target = new WantsContext();
            Resolve(target, "manager", context);

            Assert.That(target.Context, Is.SameAs(context));
        }

        [Test]
        public void Diagnostics_do_not_leak_from_one_resolve_call_into_the_next()
        {
            Resolve(new IndexTooHigh(), "sword", 12);
            Assert.That(_resolver.Diagnostics.Count, Is.EqualTo(1));

            Resolve(new IndexedInts(), 3, 7);
            Assert.That(_resolver.Diagnostics, Is.Empty);
        }

        [Test]
        public void An_empty_payload_assigns_nothing_and_reports_nothing()
        {
            var target = new IndexedInts();
            Resolve(target);

            Assert.That(target.X, Is.EqualTo(0));
            Assert.That(_resolver.Diagnostics, Is.Empty);
        }

        [Test]
        public void Indexed_properties_resolve_before_unindexed_ones_whatever_the_declaration_order()
        {
            var target = new ImplicitThenExplicit();
            Resolve(target, 3, 7);

            // A single pass in declaration order would hand _next the first int and
            // leave _first duplicate-claiming it. Two phases give _first candidate 0
            // and _next whatever is left.
            Assert.That(target.First, Is.EqualTo(3));
            Assert.That(target.Next, Is.EqualTo(7));
            Assert.That(_resolver.Diagnostics, Is.Empty);
        }

        [Test]
        public void A_negative_index_reports_IndexOutOfRange()
        {
            Resolve(new NegativeIndex(), 3, 7);

            Assert.That(_resolver.Diagnostics.Count, Is.EqualTo(1));
            Assert.That(_resolver.Diagnostics[0].Kind,
                Is.EqualTo(SignalParamDiagnosticKind.IndexOutOfRange));
            Assert.That(_resolver.Diagnostics[0].RequestedIndex, Is.EqualTo(-1));
        }

        [Test]
        public void A_trailing_null_is_taken_by_the_next_unindexed_property()
        {
            var target = new WeaponAndSkin();
            Resolve(target, "a", null);

            Assert.That(target.Weapon, Is.EqualTo("a"));
            Assert.That(target.Skin, Is.Null);
            Assert.That(_resolver.Diagnostics, Is.Empty);
        }

        [Test]
        public void A_shorter_payload_after_a_longer_one_does_not_inherit_stale_claims()
        {
            Resolve(new MixedPayload(), "sword", 12, 3);

            var target = new IndexedInts();
            Resolve(target, 3, 7);

            Assert.That(target.X, Is.EqualTo(3));
            Assert.That(target.Y, Is.EqualTo(7));
            Assert.That(_resolver.Diagnostics, Is.Empty);
        }
    }
}
