using System.Collections.Generic;
using FlowIoC.Editor.ModuleInstall;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class ModuleInstallStateTests
    {
        private static readonly IReadOnlyList<string> Nothing = new string[0];

        private static ModuleInstallState State(
            bool payloadResolved, bool installed, IReadOnlyList<string> missing) =>
            new ModuleInstallState(payloadResolved, installed, missing);

        [Test]
        public void A_module_that_can_be_installed_offers_to_be()
        {
            ModuleInstallState state = State(true, false, Nothing);

            Assert.AreEqual("Install", state.Label);
            Assert.IsTrue(state.Enabled);
            Assert.IsNull(state.Note);
        }

        [Test]
        public void A_module_already_in_the_project_offers_nothing()
        {
            ModuleInstallState state = State(true, true, Nothing);

            Assert.AreEqual("Installed", state.Label);
            Assert.IsFalse(state.Enabled);
        }

        /// <summary>
        /// Nothing the Editor can do will conjure a paid asset, so a missing assembly disables
        /// the button outright rather than offering to fetch anything.
        /// </summary>
        [Test]
        public void A_module_whose_paid_asset_is_absent_cannot_be_installed()
        {
            ModuleInstallState state =
                State(true, false, new[] {"Sirenix.OdinInspector.Attributes"});

            Assert.AreEqual("Missing", state.Label);
            Assert.IsFalse(state.Enabled);
            StringAssert.Contains("Sirenix.OdinInspector.Attributes", state.Note);
        }

        [Test]
        public void Every_absent_assembly_is_named_in_the_note()
        {
            ModuleInstallState state = State(true, false, new[] {"DOTween", "Shapes"});

            StringAssert.Contains("DOTween", state.Note);
            StringAssert.Contains("Shapes", state.Note);
        }

        /// <summary>
        /// A module already installed is installed whatever else is missing - the project is
        /// compiling, so the asset is evidently there.
        /// </summary>
        [Test]
        public void An_installed_module_is_not_reported_as_missing_anything()
        {
            ModuleInstallState state = State(true, true, new[] {"DOTween"});

            Assert.AreEqual("Installed", state.Label);
        }

        [Test]
        public void A_page_with_no_package_behind_it_says_so()
        {
            ModuleInstallState state = State(false, false, Nothing);

            Assert.AreEqual("Unavailable", state.Label);
            Assert.IsFalse(state.Enabled);
            StringAssert.Contains("package", state.Note);
        }
    }
}
