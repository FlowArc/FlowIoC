using System.Collections.Generic;
using FlowIoC.Editor.Help;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class PrivateModulePageTests
    {
        /// <summary>
        /// A page written the way an addon package would write one: a title, the folder it
        /// installs, and a body. Everything else is left at its default.
        /// </summary>
        private class SparsePage : PrivateModulePage
        {
            public override string Title => "Ads";

            public override string ModuleFolderName => "AdsModule";

            public override void DrawBody(HelpPainter painter)
            {
            }
        }

        private class DemandingPage : PrivateModulePage
        {
            public override string Title => "Inspector Extras";

            public override string ModuleFolderName => "InspectorExtrasModule";

            public override IReadOnlyList<string> RequiredAssemblies =>
                new[] {"Sirenix.OdinInspector.Attributes"};

            public override IReadOnlyList<string> RequiredPackages =>
                new[] {"com.unity.cinemachine"};

            public override void DrawBody(HelpPainter painter)
            {
            }
        }

        [Test]
        public void A_page_that_declares_only_what_it_must_has_no_subtitle()
        {
            Assert.AreEqual(string.Empty, new SparsePage().Subtitle);
        }

        [Test]
        public void A_page_that_names_no_icon_still_names_one()
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(new SparsePage().Icon));
        }

        [Test]
        public void A_page_asks_for_nothing_unless_it_says_otherwise()
        {
            CollectionAssert.IsEmpty(new SparsePage().RequiredAssemblies);
            CollectionAssert.IsEmpty(new SparsePage().RequiredPackages);
            CollectionAssert.IsEmpty(new SparsePage().MoreTabs);
        }

        [Test]
        public void The_first_reading_of_a_page_is_called_the_introduction()
        {
            Assert.AreEqual("Introduction", new SparsePage().BodyTabTitle);
        }

        [Test]
        public void A_page_carries_the_requirements_it_declares()
        {
            CollectionAssert.AreEqual(
                new[] {"Sirenix.OdinInspector.Attributes"}, new DemandingPage().RequiredAssemblies);

            CollectionAssert.AreEqual(
                new[] {"com.unity.cinemachine"}, new DemandingPage().RequiredPackages);
        }

        /// <summary>
        /// The seam is what an addon package compiles against, so a type falling back to internal
        /// would break every page outside FlowIoC without breaking anything inside it. That is
        /// exactly the kind of change nothing else would catch.
        /// </summary>
        [Test]
        public void The_three_types_an_addon_package_needs_are_public()
        {
            Assert.IsTrue(typeof(PrivateModulePage).IsPublic, "PrivateModulePage is not public.");
            Assert.IsTrue(typeof(HelpPainter).IsPublic, "HelpPainter is not public.");
            Assert.IsTrue(typeof(HelpTab).IsPublic, "HelpTab is not public.");
        }

        /// <summary>
        /// The marks a page draws with are the seam. GetMethod without binding flags finds public
        /// methods only, so a null answer here is the assertion.
        /// </summary>
        [Test]
        public void Every_mark_a_page_draws_with_is_public()
        {
            foreach (string mark in new[]
                     {"SubHeading", "Paragraph", "Bullet", "Rule", "Note", "Space", "Code", "Image"})
            {
                Assert.IsNotNull(typeof(HelpPainter).GetMethod(mark), $"{mark} is not public.");
            }
        }

        /// <summary>
        /// And no more of the window than that. Banner, Tree and Graph take types the seam does
        /// not publish, and a page has no business drawing them.
        /// </summary>
        [Test]
        public void The_window_half_of_the_painter_stays_inside_the_package()
        {
            Assert.IsNull(typeof(HelpPainter).GetMethod("Banner"), "Banner is public.");
            Assert.IsNull(typeof(HelpPainter).GetMethod("Tree"), "Tree is public.");
            Assert.IsNull(typeof(HelpPainter).GetMethod("Graph"), "Graph is public.");
        }
    }
}
