using FlowIoC.Editor.CodeGenerator.Menus.Module.RenameModule;
using FlowIoC.Editor.Modules;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// A module name has two stems: the whole name without Module, which is what a class or a
    /// nested module has to start with to follow a rename, and the name without its kind suffix,
    /// which is what the panel's field asks for.
    /// </summary>
    public class ModuleStemsTests
    {
        private readonly ModuleStems _stems = new ModuleStems();

        [Test]
        public void The_full_stem_drops_Module_and_nothing_else()
        {
            Assert.AreEqual("Counter", _stems.FullStem("CounterModule"));
            Assert.AreEqual("CounterTest", _stems.FullStem("CounterTestModule"));
            Assert.AreEqual("GameplayScreen", _stems.FullStem("GameplayScreenModule"));
        }

        [Test]
        public void The_typed_stem_drops_the_kind_suffix()
        {
            Assert.AreEqual("Counter", _stems.TypedStem("CounterModule", ModuleKind.Main));
            Assert.AreEqual("Counter", _stems.TypedStem("CounterTestModule", ModuleKind.Test));
            Assert.AreEqual("Gameplay", _stems.TypedStem("GameplayScreenModule", ModuleKind.Screen));
            Assert.AreEqual("GameplayScreen", _stems.TypedStem("GameplayScreenTestModule", ModuleKind.Test));
        }

        [Test]
        public void A_test_module_named_without_Test_keeps_its_full_stem()
        {
            Assert.AreEqual("Odd", _stems.TypedStem("OddModule", ModuleKind.Test));
        }

        [Test]
        public void The_name_for_a_typed_stem_puts_the_kind_suffix_back()
        {
            Assert.AreEqual("TimerModule", _stems.NameFor("Timer", ModuleKind.Main));
            Assert.AreEqual("TimerModule", _stems.NameFor("Timer", ModuleKind.Sub));
            Assert.AreEqual("TimerTestModule", _stems.NameFor("Timer", ModuleKind.Test));
            Assert.AreEqual("HudScreenModule", _stems.NameFor("Hud", ModuleKind.Screen));
        }

        [Test]
        public void Carries_wants_the_whole_stem_at_the_front()
        {
            Assert.IsTrue(_stems.Carries("CounterTestModule", "Counter"));
            Assert.IsTrue(_stems.Carries("Counter", "Counter"));
            Assert.IsFalse(_stems.Carries("MatchBoardScreenModule", "Gameplay"));
            Assert.IsFalse(_stems.Carries("", "Gameplay"));
            Assert.IsFalse(_stems.Carries("Gameplay", ""));
        }

        [Test]
        public void Carried_replaces_the_stem_at_the_front_and_keeps_the_rest()
        {
            Assert.AreEqual("TimerServiceRoot", _stems.Carried("CounterServiceRoot", "Counter", "Timer"));
            Assert.AreEqual("HudScreenTestModule", _stems.Carried("GameplayScreenTestModule", "GameplayScreen", "HudScreen"));
        }

        [Test]
        public void An_invalid_typed_stem_says_why_and_a_valid_one_says_nothing()
        {
            Assert.IsNotNull(_stems.WhyInvalid(""));
            Assert.IsNotNull(_stems.WhyInvalid("  "));
            Assert.IsNotNull(_stems.WhyInvalid("1Timer"));
            Assert.IsNotNull(_stems.WhyInvalid("Ti mer"));
            Assert.IsNotNull(_stems.WhyInvalid("TimerModule"));
            Assert.IsNull(_stems.WhyInvalid("Timer"));
            Assert.IsNull(_stems.WhyInvalid("_Timer2"));
        }
    }
}
