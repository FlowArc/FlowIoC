using System.Collections.Generic;
using FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// Which Root a generated screen context is attached to.
    ///
    /// The failure this exists for: the pick used to be the first prefab under the parent's Prefabs
    /// folder that carried a RootBase, and MainModule/Prefabs holds MainRoot.prefab beside
    /// PoolServiceRoot.prefab. It was right only because the filesystem happened to return MainRoot
    /// first. A parent whose folder holds another Root earlier in that order would have had its
    /// screen attached to the wrong Root, silently.
    /// </summary>
    public class ParentRootPrefabPickTests
    {
        private static string Picked(string moduleName, params string[] paths)
        {
            return new ParentRootPrefabPick().From(new List<string>(paths), moduleName, out string _);
        }

        private static string Refusal(string moduleName, params string[] paths)
        {
            new ParentRootPrefabPick().From(new List<string>(paths), moduleName, out string refusal);

            return refusal;
        }

        [Test]
        public void The_only_Root_prefab_in_the_module_is_taken()
        {
            Assert.AreEqual(
                "Assets/Modules/GameplayModule/Prefabs/GameplayRoot.prefab",
                Picked("GameplayModule", "Assets/Modules/GameplayModule/Prefabs/GameplayRoot.prefab"));
        }

        /// <summary>
        /// The case that was wrong. Both prefabs carry a RootBase and PoolServiceRoot is named
        /// first, so anything that takes the first one takes the wrong one.
        /// </summary>
        [Test]
        public void The_Root_named_after_the_module_wins_over_another_Root_beside_it()
        {
            Assert.AreEqual(
                "Assets/Modules/MainModule/Prefabs/MainRoot.prefab",
                Picked(
                    "MainModule",
                    "Assets/Modules/MainModule/Prefabs/PoolServiceRoot.prefab",
                    "Assets/Modules/MainModule/Prefabs/MainRoot.prefab"));
        }

        /// <summary>
        /// A module's Root carries the suffix of what it roots - CounterModule holds
        /// CounterServiceRoot, PlayerModule holds PlayerSystemRoot - so the match is on the name the
        /// module starts with rather than on the whole file name.
        /// </summary>
        [Test]
        public void A_Service_Root_is_recognised_as_the_modules_own()
        {
            Assert.AreEqual(
                "Assets/Modules/CounterModule/Prefabs/CounterServiceRoot.prefab",
                Picked(
                    "CounterModule",
                    "Assets/Modules/CounterModule/Prefabs/AnotherRoot.prefab",
                    "Assets/Modules/CounterModule/Prefabs/CounterServiceRoot.prefab"));
        }

        [Test]
        public void A_System_Root_is_recognised_as_the_modules_own()
        {
            Assert.AreEqual(
                "Assets/Modules/PlayerModule/Prefabs/PlayerSystemRoot.prefab",
                Picked(
                    "PlayerModule",
                    "Assets/Modules/PlayerModule/Prefabs/AnotherRoot.prefab",
                    "Assets/Modules/PlayerModule/Prefabs/PlayerSystemRoot.prefab"));
        }

        [Test]
        public void No_Root_prefab_at_all_is_refused()
        {
            Assert.IsNull(Picked("MainModule"));
            Assert.IsNotEmpty(Refusal("MainModule"));
        }

        /// <summary>
        /// Being silently wrong is worse than leaving the step to Add Sub Context, so an ambiguous
        /// folder attaches nothing and says which prefabs it could not choose between.
        /// </summary>
        [Test]
        public void Several_Roots_and_none_named_after_the_module_is_refused()
        {
            Assert.IsNull(
                Picked(
                    "MainModule",
                    "Assets/Modules/MainModule/Prefabs/PoolServiceRoot.prefab",
                    "Assets/Modules/MainModule/Prefabs/CameraRoot.prefab"));
        }

        [Test]
        public void The_refusal_names_the_prefabs_it_could_not_choose_between()
        {
            string refusal = Refusal(
                "MainModule",
                "Assets/Modules/MainModule/Prefabs/PoolServiceRoot.prefab",
                "Assets/Modules/MainModule/Prefabs/CameraRoot.prefab");

            StringAssert.Contains("PoolServiceRoot", refusal);
            StringAssert.Contains("CameraRoot", refusal);
        }

        [Test]
        public void Two_Roots_that_both_look_like_the_modules_own_are_refused()
        {
            Assert.IsNull(
                Picked(
                    "MainModule",
                    "Assets/Modules/MainModule/Prefabs/MainRoot.prefab",
                    "Assets/Modules/MainModule/Prefabs/MainSystemRoot.prefab"));
        }

        [Test]
        public void A_pick_that_succeeds_refuses_nothing()
        {
            Assert.IsNull(Refusal("GameplayModule", "Assets/Modules/GameplayModule/Prefabs/GameplayRoot.prefab"));
        }
    }
}
