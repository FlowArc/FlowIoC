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
                _builder.Build("Tick", "AtHome:Update ()", "Command"),
                _builder.Build("Tick", "AtHome:Update ()", "Command"));
        }

        [Test]
        public void A_different_message_is_a_different_key()
        {
            Assert.AreNotEqual(
                _builder.Build("Tick", "AtHome:Update ()", "Command"),
                _builder.Build("Tock", "AtHome:Update ()", "Command"));
        }

        /// <summary>
        /// The same sentence on two channels is two different things to a reader, so folding
        /// them together would hide one of them behind the other's count.
        /// </summary>
        [Test]
        public void The_same_message_on_two_channels_is_two_keys()
        {
            Assert.AreNotEqual(
                _builder.Build("Tick", "AtHome:Update ()", "Command"),
                _builder.Build("Tick", "AtHome:Update ()", "Screen"));
        }

        [Test]
        public void Two_call_sites_writing_the_same_sentence_stay_apart()
        {
            Assert.AreNotEqual(
                _builder.Build("Tick", "AtHome:Update ()", "Command"),
                _builder.Build("Tick", "Away:Update ()", "Command"));
        }

        [Test]
        public void A_missing_stack_trace_is_tolerated()
        {
            Assert.DoesNotThrow(() => _builder.Build("Tick", null, "Command"));
            Assert.AreEqual(_builder.Build("Tick", null, "Command"), _builder.Build("Tick", null, "Command"));
        }

        [Test]
        public void A_missing_message_is_tolerated()
        {
            Assert.DoesNotThrow(() => _builder.Build(null, null, "Command"));
        }
    }
}
