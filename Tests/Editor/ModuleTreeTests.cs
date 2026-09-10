using System.Collections.Generic;
using System.Linq;
using FlowIoC.Editor.ModuleScanner;
using FlowIoC.Editor.Modules;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The tree turns the flat list of rows a scan produces into the order the window draws them
    /// in: a module under the module it lives in. The rows arrive in any order, carrying only the
    /// name of their parent, and the tree is what turns that name into a depth, a place in the
    /// list and the guide lines a row is drawn with.
    /// </summary>
    public class ModuleTreeTests
    {
        private static ModuleRowEVO Row(
            string name, ModuleKind kind = ModuleKind.Main, string parent = null,
            ModuleCheckStatus status = ModuleCheckStatus.Ok)
        {
            var row = new ModuleRowEVO {Name = name, Kind = kind, ParentName = parent};

            row.Findings.Add(new FindingEVO("check", status, name));

            return row;
        }

        private static List<string> Names(IEnumerable<ModuleTreeRowEVO> tree) =>
            tree.Select(entry => entry.Row.Name).ToList();

        [Test]
        public void A_module_sits_under_the_module_it_lives_in()
        {
            List<ModuleTreeRowEVO> tree = new ModuleTree().Build(new[]
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
            List<ModuleTreeRowEVO> tree = new ModuleTree().Build(new[]
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
            List<ModuleTreeRowEVO> tree = new ModuleTree().Build(new[]
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
            List<ModuleTreeRowEVO> tree = new ModuleTree().Build(new[]
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
            List<ModuleTreeRowEVO> tree = new ModuleTree().Build(new[]
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
            List<ModuleTreeRowEVO> tree = new ModuleTree().Build(new[]
            {
                Row("PlayerModule"),
                Row("LostTestModule", ModuleKind.Test, "GoneModule")
            });

            CollectionAssert.AreEqual(new[] {"PlayerModule", "LostTestModule"}, Names(tree));
            Assert.AreEqual(0, tree[1].Depth);
            Assert.IsNull(tree[1].Parent);
        }

        /// <summary>
        /// "Only issues" keeps a row whose descendant has something to say, so the indent under
        /// it still has something to hang from. The issue rises through every ancestor and no
        /// further: a sibling of the broken row stays hidden.
        /// </summary>
        [Test]
        public void An_issue_rises_from_a_descendant_to_every_ancestor()
        {
            List<ModuleTreeRowEVO> tree = new ModuleTree().Build(new[]
            {
                Row("PlayerModule"),
                Row("HudScreenModule", ModuleKind.Screen, "PlayerModule"),
                Row("HudScreenTestModule", ModuleKind.Test, "HudScreenModule", ModuleCheckStatus.Manual),
                Row("PlayerTestModule", ModuleKind.Test, "PlayerModule"),
                Row("ShopModule")
            });

            Assert.IsTrue(tree[0].HasIssue, "PlayerModule");
            Assert.IsTrue(tree[1].HasIssue, "HudScreenModule");
            Assert.IsTrue(tree[2].HasIssue, "HudScreenTestModule");
            Assert.IsFalse(tree[3].HasIssue, "PlayerTestModule");
            Assert.IsFalse(tree[4].HasIssue, "ShopModule");
        }

        [Test]
        public void Every_row_is_drawn_exactly_once()
        {
            List<ModuleTreeRowEVO> tree = new ModuleTree().Build(new[]
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