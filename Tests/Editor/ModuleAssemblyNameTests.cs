using FlowIoC.Editor.CodeGenerator.Menus.Module;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class ModuleAssemblyNameTests
    {
        [Test]
        public void A_plain_module_drops_its_Module_suffix()
        {
            string result = new ModuleAssemblyName().From("PlayerModule");

            Assert.AreEqual("Modules.Player", result);
        }

        [Test]
        public void A_screen_module_names_its_parent_and_ends_in_Screen()
        {
            string result = new ModuleAssemblyName().From("MatchBoardScreenModule");

            Assert.AreEqual("Modules.MatchBoard.Screen", result);
        }

        [Test]
        public void A_test_module_names_its_parent_and_ends_in_Test()
        {
            string result = new ModuleAssemblyName().From("MatchBoardTestModule");

            Assert.AreEqual("Modules.MatchBoard.Test", result);
        }

        /// <summary>
        /// The whole suffix is sixteen characters. Two of the four copies this class replaces
        /// stripped twelve, which left the tail of the parent's name glued to the front:
        /// "Modules.MatchBoardScre.Screen.Test".
        /// </summary>
        [Test]
        public void A_screen_test_module_strips_the_whole_suffix_from_its_parent()
        {
            string result = new ModuleAssemblyName().From("MatchBoardScreenTestModule");

            Assert.AreEqual("Modules.MatchBoard.Screen.Test", result);
        }

        /// <summary>
        /// The bug this class exists for. A module called exactly "ScreenModule" matches the
        /// screen-module rule as a whole string, so the parent it would name is empty - and the
        /// four hand-rolled copies happily wrote "Modules." + "" + ".Screen". A module named after
        /// the role itself is an ordinary module, not a screen of some nameless parent.
        /// </summary>
        [Test]
        public void A_module_named_ScreenModule_is_an_ordinary_module()
        {
            string result = new ModuleAssemblyName().From("ScreenModule");

            Assert.AreEqual("Modules.Screen", result);
        }

        [Test]
        public void A_module_named_TestModule_is_an_ordinary_module()
        {
            string result = new ModuleAssemblyName().From("TestModule");

            Assert.AreEqual("Modules.Test", result);
        }

        /// <summary>
        /// "ScreenTestModule" has an empty parent under the screen-test rule, but it is not
        /// meaningless: it is what Create Module names the test module of a module called
        /// "ScreenModule", the same way CountdownServiceModule's is CountdownServiceTestModule.
        /// So the rule that no longer applies gives way to the one that does.
        /// </summary>
        [Test]
        public void A_module_named_ScreenTestModule_is_the_test_module_of_Screen()
        {
            string result = new ModuleAssemblyName().From("ScreenTestModule");

            Assert.AreEqual("Modules.Screen.Test", result);
        }

        [Test]
        public void A_name_that_does_not_end_in_Module_is_used_as_it_stands()
        {
            string result = new ModuleAssemblyName().From("Player");

            Assert.AreEqual("Modules.Player", result);
        }

        [Test]
        public void Surrounding_whitespace_is_trimmed_away()
        {
            string result = new ModuleAssemblyName().From("  PlayerModule  ");

            Assert.AreEqual("Modules.Player", result);
        }

        /// <summary>
        /// The suffix is recognised whatever its casing, but the part that survives keeps the
        /// casing the folder was given - renaming a module is not this class's job.
        /// </summary>
        [Test]
        public void The_suffix_is_matched_regardless_of_case_and_the_rest_is_left_alone()
        {
            string result = new ModuleAssemblyName().From("playermodule");

            Assert.AreEqual("Modules.player", result);
        }

        [Test]
        public void Nothing_to_name_yields_nothing()
        {
            Assert.AreEqual(string.Empty, new ModuleAssemblyName().From(null));
            Assert.AreEqual(string.Empty, new ModuleAssemblyName().From("   "));
        }
    }
}