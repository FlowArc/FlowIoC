using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.Injectable.Utils;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class SignalParamEntryBuilderTests
    {
        private class BaseTarget
        {
            [SignalParam] private string _first { get; set; }
            [SignalParam] private string _second { get; set; }
        }

        private class DerivedTarget : BaseTarget
        {
            [SignalParam(2)] private int _third { get; set; }
            private int _ignored { get; set; }
        }

        private class VirtualTarget
        {
            [SignalParam] protected virtual string Value { get; set; }
        }

        private class OverridingTarget : VirtualTarget
        {
            protected override string Value { get; set; }
        }

        private class AnnotatedVirtualTarget
        {
            [SignalParam] protected virtual string Value { get; set; }
        }

        private class AnnotatedOverridingTarget : AnnotatedVirtualTarget
        {
            [SignalParam(1)] protected override string Value { get; set; }
        }

        [Test]
        public void Build_lists_base_properties_before_derived_ones_in_source_order()
        {
            var entries = new SignalParamEntryBuilder().Build(typeof(DerivedTarget));

            CollectionAssert.AreEqual(
                new[] { "_first", "_second", "_third" },
                entries.ConvertAll(entry => entry.Property.Name));
        }

        [Test]
        public void Build_skips_properties_without_the_attribute()
        {
            var entries = new SignalParamEntryBuilder().Build(typeof(DerivedTarget));

            Assert.That(entries.Exists(entry => entry.Property.Name == "_ignored"), Is.False);
        }

        [Test]
        public void Build_records_whether_an_index_was_written()
        {
            var entries = new SignalParamEntryBuilder().Build(typeof(DerivedTarget));

            SignalParamEntry first = entries.Find(entry => entry.Property.Name == "_first");
            SignalParamEntry third = entries.Find(entry => entry.Property.Name == "_third");

            Assert.That(first.HasIndex, Is.False);
            Assert.That(first.Index, Is.EqualTo(0));
            Assert.That(third.HasIndex, Is.True);
            Assert.That(third.Index, Is.EqualTo(2));
        }

        [Test]
        public void Build_records_the_property_type()
        {
            var entries = new SignalParamEntryBuilder().Build(typeof(DerivedTarget));

            Assert.That(entries.Find(entry => entry.Property.Name == "_third").Type,
                Is.EqualTo(typeof(int)));
        }

        [Test]
        public void Build_records_an_overridden_property_once()
        {
            var entries = new SignalParamEntryBuilder().Build(typeof(OverridingTarget));

            Assert.That(entries.Count, Is.EqualTo(1));
            Assert.That(entries[0].Property.DeclaringType, Is.EqualTo(typeof(VirtualTarget)));
        }

        [Test]
        public void Build_records_a_property_once_even_when_the_override_is_also_annotated()
        {
            var entries = new SignalParamEntryBuilder().Build(typeof(AnnotatedOverridingTarget));

            Assert.That(entries.Count, Is.EqualTo(1));
            Assert.That(entries[0].Property.DeclaringType, Is.EqualTo(typeof(AnnotatedVirtualTarget)));
            Assert.That(entries[0].HasIndex, Is.False);
        }

        [Test]
        public void Build_returns_an_empty_list_for_a_null_type()
        {
            Assert.That(new SignalParamEntryBuilder().Build(null), Is.Empty);
        }
    }
}
