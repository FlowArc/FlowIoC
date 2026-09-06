using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlowIoC.Editor.ModuleCards;
using FlowIoC.Editor.Modules;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class ModuleChildrenTests
    {
        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "FlowIoCModuleChildren" + Path.GetRandomFileName());

            Directory.CreateDirectory(Path.Combine(_root, "PlayerModule", "zScreenModules", "PlayerScreenModule"));
            Directory.CreateDirectory(Path.Combine(_root, "PlayerModule", "zSubModules", "PlayerInputModule"));
            Directory.CreateDirectory(Path.Combine(_root, "PlayerModule", "zTestModules", "PlayerTestModule"));
            Directory.CreateDirectory(Path.Combine(
                _root, "PlayerModule", "zScreenModules", "PlayerScreenModule", "zTestModules",
                "PlayerScreenTestModule"));
            Directory.CreateDirectory(Path.Combine(_root, "PlayerModule", "Scripts", "Runtime", "Models"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }

        private List<string> NamesUnder(params string[] segments)
        {
            string path = Path.Combine(new[] {_root}.Concat(segments).ToArray());

            return new ModuleChildren().Of(path).Select(child => child.Name + " (" + child.Kind + ")").ToList();
        }

        [Test]
        public void A_modules_own_children_are_found_with_their_kind()
        {
            List<string> names = NamesUnder("PlayerModule");

            CollectionAssert.Contains(names, "PlayerScreenModule (Screen)");
            CollectionAssert.Contains(names, "PlayerInputModule (Sub)");
            CollectionAssert.Contains(names, "PlayerTestModule (Test)");
        }

        /// <summary>
        /// A test module under a screen module belongs to that screen module. Naming it on the
        /// grandparent's card as well would say the module owns something it does not.
        /// </summary>
        [Test]
        public void A_grandchild_belongs_to_the_module_that_holds_it_and_not_to_the_one_above()
        {
            CollectionAssert.DoesNotContain(NamesUnder("PlayerModule"), "PlayerScreenTestModule (Test)");

            CollectionAssert.Contains(
                NamesUnder("PlayerModule", "zScreenModules", "PlayerScreenModule"),
                "PlayerScreenTestModule (Test)");
        }

        [Test]
        public void An_ordinary_folder_inside_the_module_is_not_a_child()
        {
            Assert.AreEqual(3, NamesUnder("PlayerModule").Count);
        }

        [Test]
        public void A_module_folder_that_is_not_there_yields_nothing()
        {
            Assert.IsEmpty(new ModuleChildren().Of(Path.Combine(_root, "NoSuchModule")));
        }

        [Test]
        public void Each_child_carries_the_path_it_was_found_at()
        {
            ScannedModule screen = new ModuleChildren().Of(Path.Combine(_root, "PlayerModule"))
                .First(child => child.Name == "PlayerScreenModule");

            StringAssert.EndsWith("PlayerScreenModule", screen.AbsolutePath);
        }
    }
}