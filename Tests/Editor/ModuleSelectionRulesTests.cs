using FlowIoC.Editor.CodeGenerator.Menus.Module;
using FlowIoC.Editor.Modules;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class ModuleSelectionRulesTests
    {
        /// <summary>
        /// The full created-kind by parent-kind table. A new Main module cannot nest under a
        /// Screen or Test module; a new Screen module belongs to the module whose feature it shows,
        /// so it never nests under a Screen or a Test module; a new Test module cannot nest under
        /// another Test module - it attaches to the module it tests, not to a peer test module. A
        /// new Sub module has no restriction at all. This is the one rule in the module-hierarchy
        /// tree that regressed silently during the shared-drawer refactor, so every one of the
        /// sixteen combinations is pinned here rather than spot-checked.
        /// </summary>
        [TestCase(ModuleKind.Main, ModuleKind.Main, true)]
        [TestCase(ModuleKind.Main, ModuleKind.Sub, true)]
        [TestCase(ModuleKind.Main, ModuleKind.Screen, false)]
        [TestCase(ModuleKind.Main, ModuleKind.Test, false)]
        [TestCase(ModuleKind.Sub, ModuleKind.Main, true)]
        [TestCase(ModuleKind.Sub, ModuleKind.Sub, true)]
        [TestCase(ModuleKind.Sub, ModuleKind.Screen, true)]
        [TestCase(ModuleKind.Sub, ModuleKind.Test, true)]
        [TestCase(ModuleKind.Screen, ModuleKind.Main, true)]
        [TestCase(ModuleKind.Screen, ModuleKind.Sub, true)]
        [TestCase(ModuleKind.Screen, ModuleKind.Screen, false)]
        [TestCase(ModuleKind.Screen, ModuleKind.Test, false)]
        [TestCase(ModuleKind.Test, ModuleKind.Main, true)]
        [TestCase(ModuleKind.Test, ModuleKind.Sub, true)]
        [TestCase(ModuleKind.Test, ModuleKind.Screen, true)]
        [TestCase(ModuleKind.Test, ModuleKind.Test, false)]
        public void CanHost_matches_the_creation_rule_table(object created, object parent, bool expected)
        {
            Assert.AreEqual(expected, new ModuleSelectionRules().CanHost((ModuleKind) created, (ModuleKind) parent));
        }
    }
}
