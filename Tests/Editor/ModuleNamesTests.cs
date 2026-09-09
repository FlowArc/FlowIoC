using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.CodeGenerator.Menus.Module;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The module a folder is and every module nested inside it, which is what Delete Module's
    /// name-keyed clean-up passes run over.
    ///
    /// Those two used to ask only about the module being deleted. A main module holds screen
    /// modules and each of those was registered in an Addressables group named after itself and
    /// given a FlowLogType channel of its own, so deleting the parent left an empty Local_Screen-
    /// group with its schema assets and a column in the Filters panel that nothing writes to.
    /// </summary>
    public class ModuleNamesTests
    {
        private const string MODULE = "C:/Project/Assets/Modules/MatchBoardModule";

        private static ModuleNames Holding(params string[] directories) =>
            new ModuleNames(_ => directories);

        [Test]
        public void The_module_itself_comes_first()
        {
            IReadOnlyList<string> names = Holding().Of(MODULE, "MatchBoardModule");

            CollectionAssert.AreEqual(new[] {"MatchBoardModule"}, names);
        }

        /// <summary>
        /// The regression this exists for. The screen module inside a main module owns an
        /// Addressables group and a log channel of its own, and both go with the folder.
        /// </summary>
        [Test]
        public void A_nested_module_is_among_them()
        {
            IReadOnlyList<string> names = Holding(
                    MODULE + "/zScreenModules",
                    MODULE + "/zScreenModules/MatchBoardScreenModule",
                    MODULE + "/zScreenModules/MatchBoardScreenModule/Scripts")
                .Of(MODULE, "MatchBoardModule");

            CollectionAssert.AreEqual(new[] {"MatchBoardModule", "MatchBoardScreenModule"}, names);
        }

        /// <summary>
        /// A screen module brings a test module, and a main module may hold several screens with
        /// one each. The walk is over the whole folder rather than one level of it.
        /// </summary>
        [Test]
        public void A_module_nested_two_deep_is_among_them()
        {
            IReadOnlyList<string> names = Holding(
                    MODULE + "/zScreenModules/MatchBoardScreenModule",
                    MODULE + "/zScreenModules/MatchBoardScreenModule/zTestModules/MatchBoardScreenTestModule")
                .Of(MODULE, "MatchBoardModule");

            CollectionAssert.Contains(names, "MatchBoardScreenTestModule");
        }

        /// <summary>
        /// The container folders are the reason the rule can be the folder name alone: they are
        /// named in the plural, so nothing has to know what they are to skip them.
        /// </summary>
        [Test]
        public void A_container_folder_names_no_module()
        {
            IReadOnlyList<string> names = Holding(
                    MODULE + "/zScreenModules",
                    MODULE + "/zSubModules",
                    MODULE + "/zTestModules")
                .Of(MODULE, "MatchBoardModule");

            CollectionAssert.AreEqual(new[] {"MatchBoardModule"}, names);
        }

        [Test]
        public void An_ordinary_folder_names_no_module()
        {
            IReadOnlyList<string> names = Holding(
                    MODULE + "/Scripts",
                    MODULE + "/Scripts/Runtime",
                    MODULE + "/Prefabs")
                .Of(MODULE, "MatchBoardModule");

            CollectionAssert.AreEqual(new[] {"MatchBoardModule"}, names);
        }

        /// <summary>
        /// The suffix has to be more than the whole name. Stripping Module off a folder called
        /// Module leaves nothing to look a group or a channel up by.
        /// </summary>
        [Test]
        public void A_folder_called_only_Module_names_no_module()
        {
            IReadOnlyList<string> names = Holding(MODULE + "/Module").Of(MODULE, "MatchBoardModule");

            CollectionAssert.AreEqual(new[] {"MatchBoardModule"}, names);
        }

        [Test]
        public void A_name_found_twice_is_answered_once()
        {
            IReadOnlyList<string> names = Holding(
                    MODULE + "/zScreenModules/MatchBoardScreenModule",
                    MODULE + "/zSubModules/MatchBoardScreenModule")
                .Of(MODULE, "MatchBoardModule");

            CollectionAssert.AreEqual(new[] {"MatchBoardModule", "MatchBoardScreenModule"}, names);
        }

        /// <summary>
        /// This runs on the way to a deletion, so a folder that cannot be walked is not a reason
        /// to leave the module's own registrations behind.
        /// </summary>
        [Test]
        public void A_folder_that_cannot_be_walked_is_skipped_rather_than_thrown()
        {
            IReadOnlyList<string> names =
                new ModuleNames(_ => throw new IOException("locked")).Of(MODULE, "MatchBoardModule");

            CollectionAssert.AreEqual(new[] {"MatchBoardModule"}, names);
        }

        [Test]
        public void A_folder_that_names_no_module_is_worth_nothing()
        {
            Assert.IsEmpty(Holding().Of(null, null));
        }
    }
}
