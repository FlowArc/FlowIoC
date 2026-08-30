using FlowIoC.Editor.SetupModules;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class SetupInstallRuleTests
    {
        private SetupInstallRule _rule;

        [SetUp]
        public void SetUp()
        {
            _rule = new SetupInstallRule();
        }

        [Test]
        public void An_empty_project_gets_the_set()
        {
            Assert.AreEqual(
                SetupInstallDecision.Install,
                _rule.For(markerPresent: false, isBatchMode: false, anyModulePresent: false));
        }

        [Test]
        public void A_project_that_already_has_modules_is_marked_and_left_alone()
        {
            Assert.AreEqual(
                SetupInstallDecision.MarkOnly,
                _rule.For(markerPresent: false, isBatchMode: false, anyModulePresent: true));
        }

        [Test]
        public void A_marked_project_is_never_offered_the_set_again()
        {
            Assert.AreEqual(
                SetupInstallDecision.Stop,
                _rule.For(markerPresent: true, isBatchMode: false, anyModulePresent: false));
        }

        [Test]
        public void A_batch_run_writes_nothing_at_all()
        {
            Assert.AreEqual(
                SetupInstallDecision.Stop,
                _rule.For(markerPresent: false, isBatchMode: true, anyModulePresent: false));
        }

        [Test]
        public void The_marker_is_checked_before_batch_mode_so_the_answer_is_the_same_either_way()
        {
            Assert.AreEqual(
                SetupInstallDecision.Stop,
                _rule.For(markerPresent: true, isBatchMode: true, anyModulePresent: true));
        }
    }
}
