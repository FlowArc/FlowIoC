using FlowIoC.Editor.Console;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class FlowConsoleAutoClearPolicyTests
    {
        private readonly FlowConsoleAutoClearPolicy _policy = new FlowConsoleAutoClearPolicy();

        [Test]
        public void Entering_play_mode_clears_only_when_that_switch_is_on()
        {
            Assert.IsTrue(_policy.ShouldClear(FlowConsoleClearTrigger.EnteringPlayMode, true, false, false));
            Assert.IsFalse(_policy.ShouldClear(FlowConsoleClearTrigger.EnteringPlayMode, false, true, true));
        }

        [Test]
        public void A_compile_clears_only_when_that_switch_is_on()
        {
            Assert.IsTrue(_policy.ShouldClear(FlowConsoleClearTrigger.CompilationStarted, false, true, false));
            Assert.IsFalse(_policy.ShouldClear(FlowConsoleClearTrigger.CompilationStarted, true, false, true));
        }

        [Test]
        public void A_build_clears_only_when_that_switch_is_on()
        {
            Assert.IsTrue(_policy.ShouldClear(FlowConsoleClearTrigger.BuildStarted, false, false, true));
            Assert.IsFalse(_policy.ShouldClear(FlowConsoleClearTrigger.BuildStarted, true, true, false));
        }

        [Test]
        public void Every_switch_off_clears_on_nothing()
        {
            Assert.IsFalse(_policy.ShouldClear(FlowConsoleClearTrigger.EnteringPlayMode, false, false, false));
            Assert.IsFalse(_policy.ShouldClear(FlowConsoleClearTrigger.CompilationStarted, false, false, false));
            Assert.IsFalse(_policy.ShouldClear(FlowConsoleClearTrigger.BuildStarted, false, false, false));
        }
    }
}
