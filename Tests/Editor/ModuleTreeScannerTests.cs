using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlowIoC.Editor.Modules;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class ModuleTreeScannerTests
    {
        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "FlowIoCModuleScan_" + Path.GetRandomFileName());
            Directory.CreateDirectory(Path.Combine(_root, "Modules"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }

        private string ModulesRoot => Path.Combine(_root, "Modules");

        private void MakeFolder(params string[] segments)
        {
            Directory.CreateDirectory(Path.Combine(new[] { ModulesRoot }.Concat(segments).ToArray()));
        }

        private List<ScannedModule> Scan()
        {
            var resolver = new ModuleKindResolver("zSubModules", "zScreenModules", "zTestModules");
            return new ModuleTreeScanner(resolver).Scan(ModulesRoot);
        }

        [Test]
        public void An_empty_modules_root_yields_nothing()
        {
            Assert.IsEmpty(Scan());
        }

        [Test]
        public void A_missing_modules_root_yields_nothing_rather_than_throwing()
        {
            Directory.Delete(ModulesRoot, true);

            Assert.IsEmpty(Scan());
        }

        [Test]
        public void A_folder_ending_in_Module_is_a_main_module()
        {
            MakeFolder("CameraModule");

            List<ScannedModule> scanned = Scan();

            Assert.AreEqual(1, scanned.Count);
            Assert.AreEqual("CameraModule", scanned[0].Name);
            Assert.AreEqual(ModuleKind.Main, scanned[0].Kind);
        }

        [Test]
        public void A_folder_that_does_not_end_in_Module_is_not_a_module()
        {
            MakeFolder("Shared");

            Assert.IsEmpty(Scan());
        }

        [Test]
        public void A_module_inside_the_screen_modules_folder_is_a_screen_module()
        {
            MakeFolder("MainModule", "zScreenModules", "HudModule");

            ScannedModule hud = Scan().Single(m => m.Name == "HudModule");

            Assert.AreEqual(ModuleKind.Screen, hud.Kind);
        }

        [Test]
        public void Both_a_parent_module_and_its_nested_module_are_found()
        {
            MakeFolder("MainModule", "zSubModules", "InventoryModule");

            CollectionAssert.AreEquivalent(
                new[] { "MainModule", "InventoryModule" },
                Scan().Select(m => m.Name).ToArray());
        }

        /// <summary>
        /// The tree is walked all the way down regardless of whether a folder is itself a
        /// module, because a module can be several folders deep — Scripts/Runtime never is one,
        /// and a screen module lives under a container folder that is not one either.
        /// </summary>
        [Test]
        public void The_walk_descends_through_folders_that_are_not_modules()
        {
            MakeFolder("MainModule", "zScreenModules", "HudModule", "zTestModules", "HudTestModule");

            ScannedModule test = Scan().Single(m => m.Name == "HudTestModule");

            Assert.AreEqual(ModuleKind.Test, test.Kind);
        }

        [Test]
        public void The_absolute_path_of_the_module_folder_comes_back_with_it()
        {
            MakeFolder("CameraModule");

            ScannedModule module = Scan().Single();

            Assert.AreEqual(Path.Combine(ModulesRoot, "CameraModule"), module.AbsolutePath);
        }
    }
}
