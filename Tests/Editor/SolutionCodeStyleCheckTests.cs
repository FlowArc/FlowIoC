using FlowIoC.Editor.ModuleScan;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class SolutionCodeStyleCheckTests
    {
        private static SolutionCodeStyleCheck.SolutionState State(
            bool drifted = false,
            string error = null,
            params string[] orphaned)
        {
            var state = new SolutionCodeStyleCheck.SolutionState {Drifted = drifted, Error = error};

            foreach (string orphan in orphaned)
                state.Orphaned.Add(orphan);

            return state;
        }

        [Test]
        public void A_solution_settings_file_matching_the_template_is_Ok()
        {
            var check = new SolutionCodeStyleCheck(() => State(), () => { });

            Assert.AreEqual(ModuleCheckStatus.Ok, check.Inspect(new ProjectTargetEVO()).Status);
        }

        [Test]
        public void A_solution_settings_file_that_has_drifted_is_Fixable()
        {
            var check = new SolutionCodeStyleCheck(() => State(drifted: true), () => { });

            FindingEVO finding = check.Inspect(new ProjectTargetEVO());

            Assert.AreEqual(ModuleCheckStatus.Fixable, finding.Status);
            StringAssert.Contains("sln.DotSettings", finding.Message);
        }

        /// <summary>
        /// A project folder renamed once leaves the old solution's settings file behind, and
        /// Rider goes on reading it.
        /// </summary>
        [Test]
        public void A_settings_file_for_a_solution_that_is_gone_is_Fixable_and_named()
        {
            var check = new SolutionCodeStyleCheck(
                () => State(orphaned: "OldName.sln.DotSettings"),
                () => { });

            FindingEVO finding = check.Inspect(new ProjectTargetEVO());

            Assert.AreEqual(ModuleCheckStatus.Fixable, finding.Status);
            StringAssert.Contains("OldName.sln.DotSettings", finding.Message);
        }

        /// <summary>
        /// A missing template is not drift the panel can repair - the package itself is
        /// incomplete - so it is reported rather than silently written over.
        /// </summary>
        [Test]
        public void A_template_that_cannot_be_read_is_Manual()
        {
            var check = new SolutionCodeStyleCheck(
                () => State(error: "FlowIoC could not find its code style"),
                () => { });

            FindingEVO finding = check.Inspect(new ProjectTargetEVO());

            Assert.AreEqual(ModuleCheckStatus.Manual, finding.Status);
            StringAssert.Contains("code style", finding.Message);
        }

        [Test]
        public void Fix_writes_the_solution_settings()
        {
            bool written = false;

            new SolutionCodeStyleCheck(() => State(drifted: true), () => written = true)
                .Fix(new ProjectTargetEVO());

            Assert.IsTrue(written);
        }
    }
}
