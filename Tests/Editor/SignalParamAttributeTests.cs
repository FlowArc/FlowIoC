using FlowIoC.BaseModule.Injectable.Attributes;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class SignalParamAttributeTests
    {
        [Test]
        public void A_bare_attribute_carries_no_index()
        {
            var attribute = new SignalParamAttribute();

            Assert.That(attribute.HasIndex, Is.False);
            Assert.That(attribute.Index, Is.EqualTo(0));
        }

        [Test]
        public void An_indexed_attribute_carries_its_index()
        {
            var attribute = new SignalParamAttribute(2);

            Assert.That(attribute.HasIndex, Is.True);
            Assert.That(attribute.Index, Is.EqualTo(2));
        }

        [Test]
        public void An_explicit_zero_index_is_not_the_same_as_no_index()
        {
            var explicitZero = new SignalParamAttribute(0);
            var bare = new SignalParamAttribute();

            Assert.That(explicitZero.HasIndex, Is.True);
            Assert.That(explicitZero.Index, Is.EqualTo(0));
            Assert.That(bare.HasIndex, Is.False);
            Assert.That(bare.Index, Is.EqualTo(0));
        }
    }
}
