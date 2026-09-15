using FlowIoC.BaseModule.Attributes;
using FlowIoC.Editor.ModulePanels;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class ModulePanelTests
    {
        /// <summary>A panel written the way a module would write one: a title, its module, rows.</summary>
        private class SparsePanel : ModulePanel
        {
            public override string Title => "Ads";

            public override string Module => "AdsModule";

            public override void Draw(ModulePanelPainter painter)
            {
            }
        }

        private abstract class HalfPanel : ModulePanel
        {
        }

        [Test]
        public void A_panel_that_declares_only_what_it_must_wears_the_plain_root_colour_and_links_no_page()
        {
            var panel = new SparsePanel();

            Assert.AreEqual(string.Empty, panel.Subtitle);
            Assert.AreEqual(FlowRole.Root, panel.Role);
            Assert.IsNull(panel.HelpPage);
        }

        [Test]
        public void An_action_can_be_pressed_and_destroys_nothing_unless_it_says_otherwise()
        {
            var action = new ModulePanelAction("Read", () => { });

            Assert.IsTrue(action.Enabled);
            Assert.IsFalse(action.Destructive);
            Assert.AreEqual("Read", action.Label);
        }

        /// <summary>
        /// The window remembers its panel as a type name across a domain reload, so the name has
        /// to come back as the panel - and a name that is not a panel any more has to come back
        /// as nothing, so the window closes instead of throwing.
        /// </summary>
        [Test]
        public void The_window_resolves_a_panel_from_its_type_name_and_nothing_from_anything_else()
        {
            Assert.AreEqual(typeof(SparsePanel), ModulePanelWindow.PanelTypeOf(typeof(SparsePanel).AssemblyQualifiedName));
            Assert.IsNull(ModulePanelWindow.PanelTypeOf(typeof(HalfPanel).AssemblyQualifiedName), "an abstract panel resolved");
            Assert.IsNull(ModulePanelWindow.PanelTypeOf(typeof(string).AssemblyQualifiedName), "a type that is no panel resolved");
            Assert.IsNull(ModulePanelWindow.PanelTypeOf("Nothing.By.This.Name, Nowhere"));
            Assert.IsNull(ModulePanelWindow.PanelTypeOf(null));
        }

        /// <summary>
        /// The seam is what a module compiles against, so a type falling back to internal would
        /// break every panel outside FlowIoC without breaking anything inside it.
        /// </summary>
        [Test]
        public void The_types_a_module_needs_are_public()
        {
            Assert.IsTrue(typeof(ModulePanel).IsPublic, "ModulePanel is not public.");
            Assert.IsTrue(typeof(ModulePanelPainter).IsPublic, "ModulePanelPainter is not public.");
            Assert.IsTrue(typeof(ModulePanelAction).IsPublic, "ModulePanelAction is not public.");
            Assert.IsTrue(typeof(ModulePanelWindow).IsPublic, "ModulePanelWindow is not public.");
            Assert.IsNotNull(typeof(ModulePanelWindow).GetMethod("Open"), "Open is not public.");
        }
    }
}
