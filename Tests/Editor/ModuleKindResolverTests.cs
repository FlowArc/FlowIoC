using FlowIoC.Editor.Modules;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class ModuleKindResolverTests
    {
        private ModuleKindResolver NewResolver()
        {
            return new ModuleKindResolver("zSubModules", "zScreenModules", "zTestModules");
        }

        [Test]
        public void A_module_directly_under_Modules_is_a_main_module()
        {
            Assert.AreEqual(ModuleKind.Main, NewResolver().Resolve("Modules"));
        }

        [Test]
        public void A_module_under_the_sub_modules_folder_is_a_sub_module()
        {
            Assert.AreEqual(ModuleKind.Sub, NewResolver().Resolve("zSubModules"));
        }

        [Test]
        public void A_module_under_the_screen_modules_folder_is_a_screen_module()
        {
            Assert.AreEqual(ModuleKind.Screen, NewResolver().Resolve("zScreenModules"));
        }

        [Test]
        public void A_module_under_the_test_modules_folder_is_a_test_module()
        {
            Assert.AreEqual(ModuleKind.Test, NewResolver().Resolve("zTestModules"));
        }

        /// <summary>
        /// The container folder names live in ED_CodeGenerator and a project may rename
        /// them. Matching on a hardcoded "z" prefix, which DeleteModuleMenu used to do, gets
        /// the kind wrong the moment a project renames zScreenModules to something else.
        /// </summary>
        [Test]
        public void The_container_folder_names_come_from_the_settings_not_from_a_z_prefix()
        {
            var resolver = new ModuleKindResolver("Nested", "Screens", "Tests");

            Assert.AreEqual(ModuleKind.Screen, resolver.Resolve("Screens"));
            Assert.AreEqual(ModuleKind.Main, resolver.Resolve("zScreenModules"));
        }

        [Test]
        public void The_comparison_ignores_case()
        {
            Assert.AreEqual(ModuleKind.Test, NewResolver().Resolve("ZTESTMODULES"));
        }

        [Test]
        public void An_unknown_parent_folder_is_a_main_module()
        {
            Assert.AreEqual(ModuleKind.Main, NewResolver().Resolve("Whatever"));
        }

        [Test]
        public void A_null_parent_folder_is_a_main_module()
        {
            Assert.AreEqual(ModuleKind.Main, NewResolver().Resolve(null));
        }
    }
}
