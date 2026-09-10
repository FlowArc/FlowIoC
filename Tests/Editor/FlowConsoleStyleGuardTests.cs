using FlowIoC.Editor.Console;
using NUnit.Framework;
using UnityEngine;

namespace FlowIoC.Tests
{
    public class FlowConsoleStyleGuardTests
    {
        private readonly FlowConsoleStyleGuard _guard = new FlowConsoleStyleGuard();

        [Test]
        public void A_style_never_built_is_not_built()
        {
            Assert.IsFalse(_guard.IsBuilt(null, "FlowConsoleFlowHeader"));
        }

        /// <summary>
        /// A style copied from an editor style that was not ready reads back with none of what was
        /// written to it - no name, no size, black text - and a domain reload carries it across as
        /// it is. The name is the one thing every build writes, so a style that does not carry it
        /// is the placeholder, whatever else it says.
        /// </summary>
        [Test]
        public void A_style_that_lost_its_name_is_the_placeholder()
        {
            Assert.IsFalse(_guard.IsBuilt(new GUIStyle(), "FlowConsoleFlowHeader"));
        }

        [Test]
        public void A_style_carrying_its_name_is_built()
        {
            Assert.IsTrue(_guard.IsBuilt(new GUIStyle {name = "FlowConsoleFlowHeader"}, "FlowConsoleFlowHeader"));
        }
    }
}
