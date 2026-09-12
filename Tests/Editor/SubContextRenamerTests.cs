using System.Collections.Generic;
using FlowIoC.BaseModule.Root;
using FlowIoC.Editor.CodeGenerator.Menus.Module.DeleteModule;
using FlowIoC.Editor.CodeGenerator.Menus.Module.RenameModule;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// A Root lists a context by its full name, which runtime resolves and nothing else. When the
    /// namespace or the class changes, the entry has to say the new name - and everything else on
    /// it, the screen override above all, has to stay exactly as the scene had it.
    /// </summary>
    public class SubContextRenamerTests
    {
        private static readonly List<TextRule> Rules = new List<TextRule>
        {
            TextRule.Token("Modules.CounterModule", "Modules.TimerModule"),
            TextRule.Token("CounterServiceContext", "TimerServiceContext")
        };

        [Test]
        public void The_two_names_follow_the_rules_and_the_rest_of_the_entry_stays()
        {
            var entry = new SubContextData
            {
                ContextFullName = "Modules.CounterModule.RootsContexts.CounterServiceContext",
                ContextName = "CounterServiceContext",
                AutoSetup = true,
                OverrideScreen = true,
                ScreenLayer = 3
            };

            SubContextData mapped = new SubContextRenamer().Mapped(entry, Rules, out bool changed);

            Assert.IsTrue(changed);
            Assert.AreEqual("Modules.TimerModule.RootsContexts.TimerServiceContext", mapped.ContextFullName);
            Assert.AreEqual("TimerServiceContext", mapped.ContextName);
            Assert.IsTrue(mapped.AutoSetup);
            Assert.IsTrue(mapped.OverrideScreen);
            Assert.AreEqual(3, mapped.ScreenLayer);
        }

        [Test]
        public void An_entry_the_rules_do_not_reach_is_unchanged()
        {
            var entry = new SubContextData {ContextFullName = "Modules.OtherModule.X.OtherContext", ContextName = "OtherContext"};

            SubContextData mapped = new SubContextRenamer().Mapped(entry, Rules, out bool changed);

            Assert.IsFalse(changed);
            Assert.AreEqual(entry.ContextFullName, mapped.ContextFullName);
        }

        [Test]
        public void The_assets_inside_the_module_are_listed_along_with_the_ones_outside()
        {
            var references = new ModuleAssetReferences(
                () => new[] {"Assets/Modules/CounterModule/Scenes/CounterTestScene.unity", "Assets/Other/Main.unity", "Assets/Other/Nope.prefab"},
                path => path.EndsWith("Nope.prefab")
                    ? new[] {"Assets/Other/Thing.cs"}
                    : new[] {"Assets/Modules/CounterModule/Scripts/Runtime/RootsContexts/CounterServiceContext.cs"});

            IReadOnlyList<string> found = references.AssetsPointingIntoOrInside("Assets/Modules/CounterModule");

            CollectionAssert.AreEquivalent(
                new[] {"Assets/Modules/CounterModule/Scenes/CounterTestScene.unity", "Assets/Other/Main.unity"}, found);
        }

        [Test]
        public void The_line_says_where_and_whether_the_scene_was_left_unsaved()
        {
            var outcome = new SubContextRenameEVO
            {
                AssetPath = "Assets/Main.unity", RootName = "GameplayRoot",
                OldName = "CounterServiceContext", NewName = "TimerServiceContext", SceneLeftUnsaved = true
            };

            Assert.AreEqual("GameplayRoot in Assets/Main.unity: CounterServiceContext → TimerServiceContext - scene not saved", outcome.Line());
        }
    }
}
