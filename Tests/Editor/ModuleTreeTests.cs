using System.Collections.Generic;
using System.Linq;
using FlowIoC.Editor.Modules;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The tree turns a flat list of modules into the order a window draws them in: a module
    /// under the module it lives in. The items arrive in any order, carrying only the name of
    /// their parent, and the tree is what turns that name into a depth, a place in the list, the
    /// entry the row hangs from and everything drawn inside it.
    /// </summary>
    public class ModuleTreeTests
    {
        private class Item : IModuleTreeItem
        {
            public string Name { get; set; }
            public ModuleKind Kind { get; set; }
            public string ParentName { get; set; }
        }

        private static Item Row(string name, ModuleKind kind = ModuleKind.Main, string parent = null) =>
            new Item {Name = name, Kind = kind, ParentName = parent};

        private static List<string> Names(IEnumerable<ModuleTreeRowEVO<Item>> tree) =>
            tree.Select(entry => entry.Row.Name).ToList();

        [Test]
        public void A_module_sits_under_the_module_it_lives_in()
        {
            List<ModuleTreeRowEVO<Item>> tree = new ModuleTree().Build(new[]
            {
                Row("PlayerTestModule", ModuleKind.Test, "PlayerModule"),
                Row("PlayerModule")
            });

            CollectionAssert.AreEqual(new[] {"PlayerModule", "PlayerTestModule"}, Names(tree));
            Assert.AreEqual(0, tree[0].Depth);
            Assert.AreEqual(1, tree[1].Depth);
        }

        [Test]
        public void Top_level_modules_are_ordered_by_name()
        {
            List<ModuleTreeRowEVO<Item>> tree = new ModuleTree().Build(new[]
            {
                Row("PlayerModule"),
                Row("InputModule")
            });

            CollectionAssert.AreEqual(new[] {"InputModule", "PlayerModule"}, Names(tree));
        }

        /// <summary>
        /// Sub modules, then screens, then tests - the order the kinds are declared in - and by
        /// name inside each kind. The badge column then reads as groups rather than as a shuffle.
        /// </summary>
        [Test]
        public void Siblings_are_ordered_by_kind_and_then_by_name()
        {
            List<ModuleTreeRowEVO<Item>> tree = new ModuleTree().Build(new[]
            {
                Row("PlayerModule"),
                Row("PlayerTestModule", ModuleKind.Test, "PlayerModule"),
                Row("ShopScreenModule", ModuleKind.Screen, "PlayerModule"),
                Row("HudScreenModule", ModuleKind.Screen, "PlayerModule"),
                Row("InventorySubModule", ModuleKind.Sub, "PlayerModule")
            });

            CollectionAssert.AreEqual(
                new[]
                {
                    "PlayerModule", "InventorySubModule", "HudScreenModule", "ShopScreenModule", "PlayerTestModule"
                },
                Names(tree));
        }

        [Test]
        public void A_module_and_everything_inside_it_come_before_the_next_sibling()
        {
            List<ModuleTreeRowEVO<Item>> tree = new ModuleTree().Build(new[]
            {
                Row("PlayerModule"),
                Row("PlayerTestModule", ModuleKind.Test, "PlayerModule"),
                Row("HudScreenModule", ModuleKind.Screen, "PlayerModule"),
                Row("HudScreenTestModule", ModuleKind.Test, "HudScreenModule")
            });

            CollectionAssert.AreEqual(
                new[] {"PlayerModule", "HudScreenModule", "HudScreenTestModule", "PlayerTestModule"},
                Names(tree));

            Assert.AreEqual(2, tree[2].Depth);
        }

        /// <summary>
        /// The guide line a row is drawn with hangs from the row of the module it lives in, so
        /// each entry points at that entry - the one the window has the rect of by the time the
        /// child is drawn.
        /// </summary>
        [Test]
        public void A_row_hangs_from_the_row_of_the_module_it_lives_in()
        {
            List<ModuleTreeRowEVO<Item>> tree = new ModuleTree().Build(new[]
            {
                Row("PlayerModule"),
                Row("HudScreenModule", ModuleKind.Screen, "PlayerModule"),
                Row("HudScreenTestModule", ModuleKind.Test, "HudScreenModule"),
                Row("ShopModule")
            });

            Assert.IsNull(tree[0].Parent);
            Assert.AreSame(tree[0], tree[1].Parent);
            Assert.AreSame(tree[1], tree[2].Parent);
            Assert.IsNull(tree[3].Parent);
        }

        /// <summary>
        /// A row naming a parent the scan did not find is still a module, so it is drawn at the
        /// top rather than dropped - a list that loses a row is worse than one that misplaces it.
        /// </summary>
        [Test]
        public void A_row_whose_parent_is_not_in_the_list_is_drawn_at_the_top()
        {
            List<ModuleTreeRowEVO<Item>> tree = new ModuleTree().Build(new[]
            {
                Row("PlayerModule"),
                Row("LostTestModule", ModuleKind.Test, "GoneModule")
            });

            CollectionAssert.AreEqual(new[] {"PlayerModule", "LostTestModule"}, Names(tree));
            Assert.AreEqual(0, tree[1].Depth);
            Assert.IsNull(tree[1].Parent);
        }

        /// <summary>
        /// What an entry carries below it, in drawing order: what Module Scanner's "Only issues"
        /// asks to keep a green parent over a red child, what Delete Module's search asks to keep
        /// a module whose child matched, and what its confirmation names as going with the module.
        /// </summary>
        [Test]
        public void Descendants_are_everything_inside_a_row_in_drawing_order()
        {
            List<ModuleTreeRowEVO<Item>> tree = new ModuleTree().Build(new[]
            {
                Row("PlayerModule"),
                Row("HudScreenModule", ModuleKind.Screen, "PlayerModule"),
                Row("HudScreenTestModule", ModuleKind.Test, "HudScreenModule"),
                Row("PlayerTestModule", ModuleKind.Test, "PlayerModule"),
                Row("ShopModule")
            });

            CollectionAssert.AreEqual(
                new[] {"HudScreenModule", "HudScreenTestModule", "PlayerTestModule"},
                Names(tree[0].Descendants));
            CollectionAssert.AreEqual(new[] {"HudScreenTestModule"}, Names(tree[1].Descendants));
            CollectionAssert.IsEmpty(tree[2].Descendants);
            CollectionAssert.IsEmpty(tree[4].Descendants);
        }

        [Test]
        public void Every_row_is_drawn_exactly_once()
        {
            List<ModuleTreeRowEVO<Item>> tree = new ModuleTree().Build(new[]
            {
                Row("PlayerModule"),
                Row("HudScreenModule", ModuleKind.Screen, "PlayerModule"),
                Row("HudScreenTestModule", ModuleKind.Test, "HudScreenModule"),
                Row("LostTestModule", ModuleKind.Test, "GoneModule")
            });

            CollectionAssert.AreEquivalent(
                new[] {"PlayerModule", "HudScreenModule", "HudScreenTestModule", "LostTestModule"},
                Names(tree));
        }
    }
}