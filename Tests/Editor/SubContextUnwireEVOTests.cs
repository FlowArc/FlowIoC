using FlowIoC.Editor.CodeGenerator.Menus.Module.DeleteModule;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The line the reader is left with after Delete Module has been through the Roots.
    ///
    /// Four outcomes and four sentences, because the difference between them is exactly what the
    /// reader has to act on: one is done, one needs the scene saved, one was declined on purpose,
    /// and one is a place to go and look.
    /// </summary>
    public class SubContextUnwireEVOTests
    {
        private static SubContextUnwireEVO Entry(SubContextUnwireOutcome outcome) =>
            new SubContextUnwireEVO
            {
                AssetPath = "Assets/Scenes/MainScene.unity",
                RootName = "MainRoot",
                ContextName = "PlayerScreenContext",
                Outcome = outcome
            };

        [Test]
        public void A_removed_entry_says_it_was_removed_and_from_where()
        {
            string line = Entry(SubContextUnwireOutcome.Removed).Line();

            StringAssert.StartsWith("Removed", line);
            StringAssert.Contains("PlayerScreenContext", line);
            StringAssert.Contains("MainRoot", line);
            StringAssert.Contains("Assets/Scenes/MainScene.unity", line);
        }

        /// <summary>
        /// The one outcome that leaves the reader something to do. Saying only "Removed" here would
        /// be a lie by omission: close the scene without saving and the entry is back.
        /// </summary>
        [Test]
        public void An_entry_removed_from_an_open_scene_says_the_scene_is_not_saved()
        {
            string line = Entry(SubContextUnwireOutcome.RemovedNotSaved).Line();

            StringAssert.StartsWith("Removed", line);
            StringAssert.Contains("not saved", line);
        }

        [Test]
        public void A_skipped_entry_says_it_was_left_where_it_is()
        {
            string line = Entry(SubContextUnwireOutcome.Skipped).Line();

            StringAssert.StartsWith("Left", line);
            StringAssert.Contains("PlayerScreenContext", line);
            StringAssert.Contains("MainRoot", line);
        }

        /// <summary>
        /// What cancelling leaves: not a thing that happened, but a place to look. So it reads as a
        /// statement about the Root rather than about an action.
        /// </summary>
        [Test]
        public void A_found_entry_names_the_Root_and_what_it_lists()
        {
            string line = Entry(SubContextUnwireOutcome.Found).Line();

            StringAssert.Contains("MainRoot", line);
            StringAssert.Contains("lists", line);
            StringAssert.Contains("PlayerScreenContext", line);
            StringAssert.DoesNotContain("Removed", line);
        }

        [Test]
        public void The_four_outcomes_read_differently_from_one_another()
        {
            string removed = Entry(SubContextUnwireOutcome.Removed).Line();
            string unsaved = Entry(SubContextUnwireOutcome.RemovedNotSaved).Line();
            string skipped = Entry(SubContextUnwireOutcome.Skipped).Line();
            string found = Entry(SubContextUnwireOutcome.Found).Line();

            Assert.AreNotEqual(removed, unsaved);
            Assert.AreNotEqual(removed, skipped);
            Assert.AreNotEqual(removed, found);
            Assert.AreNotEqual(skipped, found);
        }

        /// <summary>
        /// The order is settled-to-worst so a summary can sort by it, and every value carries its
        /// number because Unity serializes an enum as an int.
        /// </summary>
        [Test]
        public void The_outcomes_keep_the_numbers_they_were_written_with()
        {
            Assert.AreEqual(0, (int) SubContextUnwireOutcome.Found);
            Assert.AreEqual(1, (int) SubContextUnwireOutcome.Removed);
            Assert.AreEqual(2, (int) SubContextUnwireOutcome.RemovedNotSaved);
            Assert.AreEqual(3, (int) SubContextUnwireOutcome.Skipped);
        }
    }
}
