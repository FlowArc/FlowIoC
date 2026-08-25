using FlowIoC.Editor.CodeGenerator.Menus.Module;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class ModuleNamespaceBuilderTests
    {
        [Test]
        public void A_module_with_no_ancestors_is_its_own_namespace()
        {
            string result = new ModuleNamespaceBuilder().Build(new string[0], "CameraModule");

            Assert.AreEqual("Modules.CameraModule", result);
        }

        /// <summary>
        /// ModuleRegistryTests proves AncestorsOf(HudTestModule) comes back nearest first as
        /// { "HudModule", "CameraModule" }. Fed that same order here, the namespace has to read
        /// root-first with the module itself last - the one part of the walk this task's brief
        /// flagged as impossible to exercise against real project data.
        /// </summary>
        [Test]
        public void Ancestors_nest_root_first_with_the_module_itself_last()
        {
            string result = new ModuleNamespaceBuilder().Build(
                new[] {"HudModule", "CameraModule"}, "HudTestModule");

            Assert.AreEqual("Modules.CameraModule.HudModule.HudTestModule", result);
        }

        [Test]
        public void A_null_ancestor_list_is_treated_as_no_ancestors()
        {
            string result = new ModuleNamespaceBuilder().Build(null, "CameraModule");

            Assert.AreEqual("Modules.CameraModule", result);
        }

        /// <summary>
        /// Matches NamespaceUtility's legacy behaviour: a lookup that never anchors on a module
        /// falls through to the bare root rather than throwing.
        /// </summary>
        [Test]
        public void A_missing_module_name_still_yields_the_root_namespace()
        {
            string result = new ModuleNamespaceBuilder().Build(new string[0], string.Empty);

            Assert.AreEqual("Modules.", result);
        }
    }
}
