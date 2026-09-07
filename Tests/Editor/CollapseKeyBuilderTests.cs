using FlowIoC.ConsoleModule;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class CollapseKeyBuilderTests
    {
        private readonly CollapseKeyBuilder _builder = new CollapseKeyBuilder();

        [Test]
        public void The_same_log_written_twice_folds_onto_one_key()
        {
            Assert.AreEqual(
                _builder.Build("Tick", "AtHome:Update ()", 14),
                _builder.Build("Tick", "AtHome:Update ()", 14));
        }

        [Test]
        public void A_different_message_is_a_different_key()
        {
            Assert.AreNotEqual(
                _builder.Build("Tick", "AtHome:Update ()", 14),
                _builder.Build("Tock", "AtHome:Update ()", 14));
        }

        /// <summary>
        /// The same sentence on two channels is two different things to a reader, so folding
        /// them together would hide one of them behind the other's count.
        /// </summary>
        [Test]
        public void The_same_message_on_two_channels_is_two_keys()
        {
            Assert.AreNotEqual(
                _builder.Build("Tick", "AtHome:Update ()", 14),
                _builder.Build("Tick", "AtHome:Update ()", 15));
        }

        [Test]
        public void Two_call_sites_writing_the_same_sentence_stay_apart()
        {
            Assert.AreNotEqual(
                _builder.Build("Tick", "AtHome:Update ()", 14),
                _builder.Build("Tick", "Away:Update ()", 14));
        }

        [Test]
        public void A_missing_stack_trace_is_tolerated()
        {
            Assert.DoesNotThrow(() => _builder.Build("Tick", null, 14));
            Assert.AreEqual(_builder.Build("Tick", null, 14), _builder.Build("Tick", null, 14));
        }

        [Test]
        public void A_missing_message_is_tolerated()
        {
            Assert.DoesNotThrow(() => _builder.Build(null, null, 14));
        }
    }
}
