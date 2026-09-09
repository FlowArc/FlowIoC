using FlowIoC.Editor.CodeGenerator.Menus.Module;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The three modules Delete Module offers no button for. They arrive with the setup set and
    /// everything else in the project is written against them, so deleting one does not leave a
    /// smaller project - it leaves one that does not run, and the deleter is thorough enough that
    /// there is no half-way state to recover from.
    /// </summary>
    public class CoreModulesTests
    {
        private readonly CoreModules _coreModules = new CoreModules();

        [TestCase("MainModule")]
        [TestCase("ConnectorModule")]
        [TestCase("ScreenModule")]
        public void A_module_the_project_is_built_on_says_why_it_stays(string moduleName)
        {
            Assert.IsNotEmpty(_coreModules.WhyKept(moduleName));
        }

        /// <summary>
        /// GameplayModule arrives with the other three and is not one of them: it is the worked
        /// example a game replaces with its own, which is exactly what deleting it is for.
        /// </summary>
        [Test]
        public void The_example_module_that_ships_beside_them_is_deletable()
        {
            Assert.IsNull(_coreModules.WhyKept("GameplayModule"));
        }

        [Test]
        public void A_module_the_game_wrote_is_deletable()
        {
            Assert.IsNull(_coreModules.WhyKept("PlayerModule"));
        }

        /// <summary>
        /// The match is the whole folder name. A screen module of the game's own is not the screen
        /// service, and a module named after one is not it either.
        /// </summary>
        [Test]
        public void A_name_that_only_contains_one_of_them_is_deletable()
        {
            Assert.IsNull(_coreModules.WhyKept("MainScreenModule"));
            Assert.IsNull(_coreModules.WhyKept("ScreenModuleTest"));
        }

        [Test]
        public void No_name_at_all_keeps_nothing()
        {
            Assert.IsNull(_coreModules.WhyKept(null));
            Assert.IsNull(_coreModules.WhyKept(string.Empty));
        }
    }
}
