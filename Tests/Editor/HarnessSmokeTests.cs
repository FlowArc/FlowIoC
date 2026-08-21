using FlowIoC.BaseModule.Controller.CommandGroup;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class HarnessSmokeTests
    {
        // CommandGroupResolver is internal to the FlowIoC assembly, so this file only
        // compiles when [InternalsVisibleTo("FlowIoC.Tests")] is in place. That is the
        // point of the assertion: it proves the grant, not just the assembly reference.
        [Test]
        public void The_test_assembly_can_see_the_FlowIoC_runtime_assembly_internals()
        {
            Assert.That(typeof(CommandGroupResolver).Assembly.GetName().Name,
                Is.EqualTo("FlowIoC"));
        }
    }
}
