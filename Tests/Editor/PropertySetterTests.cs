using System;
using System.Reflection;
using FlowIoC.BaseModule.Injectable.Utils;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The setter delegate injection writes through. Every [Inject] and [SignalParam] property in
    /// a project is filled through one of these, private setters and value types included, so
    /// each of those shapes is written here once.
    /// </summary>
    public class PropertySetterTests
    {
        private class Target
        {
            private int _number { get; set; }
            private string _text { get; set; }
            public int? Maybe { get; set; }
            public int ReadOnly => 1;

            public int Number => _number;
            public string Text => _text;
        }

        private const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static Action<object, object> SetterFor(string property)
            => PropertySetter.For(typeof(Target).GetProperty(property, Any));

        [Test]
        public void A_private_value_type_property_is_written()
        {
            Target target = new Target();

            SetterFor("_number")(target, 7);

            Assert.That(target.Number, Is.EqualTo(7));
        }

        [Test]
        public void A_private_reference_type_property_is_written()
        {
            Target target = new Target();

            SetterFor("_text")(target, "seven");

            Assert.That(target.Text, Is.EqualTo("seven"));
        }

        [Test]
        public void A_nullable_property_takes_a_null_and_a_value()
        {
            Target target = new Target {Maybe = 3};
            Action<object, object> set = SetterFor("Maybe");

            set(target, null);
            Assert.That(target.Maybe, Is.Null);

            set(target, 5);
            Assert.That(target.Maybe, Is.EqualTo(5));
        }

        [Test]
        public void A_property_without_a_setter_has_none_to_hand_back()
        {
            Assert.That(SetterFor("ReadOnly"), Is.Null);
        }
    }
}
