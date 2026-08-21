using FlowIoC.BaseModule.Injectable.Attributes;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class HarnessSmokeTests
    {
        [Test]
        public void The_test_assembly_can_see_the_FlowIoC_runtime_assembly()
        {
            Assert.That(typeof(SignalParamAttribute).Assembly.GetName().Name,
                Is.EqualTo("FlowIoC"));
        }
    }
}
