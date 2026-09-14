using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Controller.Commands;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class CommandDisplayNameTests
    {
        private readonly CommandDisplayName _name = new();

        private interface IProbeService
        {
            public static class Commands
            {
                public class Play : Command<int>
                {
                    public override void Execute(int preset) { }
                }
            }
        }

        [Test]
        public void A_top_level_command_is_its_own_name()
        {
            Assert.AreEqual("SignalDispatchCommand", _name.Of(typeof(SignalDispatchCommand)));
        }

        /// <summary>
        /// The step a Service ships reads in a diagnostic the way its binding is written, so
        /// two Services that both ship a Play are told apart by the interface in front.
        /// </summary>
        [Test]
        public void A_step_nested_in_a_service_interface_is_named_by_the_chain_without_the_namespace()
        {
            Assert.AreEqual("CommandDisplayNameTests.IProbeService.Commands.Play",
                _name.Of(typeof(IProbeService.Commands.Play)));
        }

        [Test]
        public void No_type_is_an_empty_name_rather_than_a_throw()
        {
            Assert.AreEqual(string.Empty, _name.Of(null));
        }
    }
}
