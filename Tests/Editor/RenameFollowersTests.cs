using System.Collections.Generic;
using System.Linq;
using FlowIoC.Editor.CodeGenerator.Menus.Module;
using FlowIoC.Editor.CodeGenerator.Menus.Module.RenameModule;
using FlowIoC.Editor.Modules;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// A nested module follows a rename when its name starts with its parent's stem, through the
    /// chain: GameplayScreenTestModule follows GameplayScreenModule, which follows GameplayModule.
    /// A module that does not carry the name keeps it, and so does everything under it.
    /// </summary>
    public class RenameFollowersTests
    {
        private static ModulePickEVO Pick(string name, ModuleKind kind, string parent) =>
            new ModulePickEVO {Name = name, Kind = kind, ParentName = parent, Path = "D:/p/" + name};

        private static ModuleTreeRowEVO<ModulePickEVO> Picked(List<ModulePickEVO> picks, string name) =>
            new ModuleTree().Build(picks).First(row => row.Row.Name == name);

        private static List<ModulePickEVO> Gameplay() => new List<ModulePickEVO>
        {
            Pick("GameplayModule", ModuleKind.Main, null),
            Pick("GameplayScreenModule", ModuleKind.Screen, "GameplayModule"),
            Pick("GameplayScreenTestModule", ModuleKind.Test, "GameplayScreenModule"),
            Pick("MatchBoardScreenModule", ModuleKind.Screen, "GameplayModule"),
            Pick("MatchBoardScreenTestModule", ModuleKind.Test, "MatchBoardScreenModule"),
            Pick("OtherModule", ModuleKind.Main, null)
        };

        [Test]
        public void A_carrier_follows_through_the_chain_with_the_stem_replaced_at_the_front()
        {
            List<FollowerRenameEVO> followers = new RenameFollowers().Of(Picked(Gameplay(), "GameplayModule"), "HudModule", _ => true);

            FollowerRenameEVO screen = followers.Single(f => f.OldName == "GameplayScreenModule");
            FollowerRenameEVO test = followers.Single(f => f.OldName == "GameplayScreenTestModule");

            Assert.AreEqual("HudScreenModule", screen.NewName);
            Assert.IsTrue(screen.Follows);
            Assert.AreEqual("HudScreenTestModule", test.NewName);
            Assert.IsTrue(test.Follows);
        }

        [Test]
        public void A_module_that_does_not_carry_the_name_is_not_listed_and_neither_is_what_it_holds()
        {
            List<FollowerRenameEVO> followers = new RenameFollowers().Of(Picked(Gameplay(), "GameplayModule"), "HudModule", _ => true);

            Assert.IsFalse(followers.Any(f => f.OldName.StartsWith("MatchBoard")));
            Assert.IsFalse(followers.Any(f => f.OldName == "OtherModule"));
        }

        [Test]
        public void An_unticked_carrier_keeps_its_name_and_takes_what_it_holds_with_it()
        {
            List<FollowerRenameEVO> followers = new RenameFollowers().Of(
                Picked(Gameplay(), "GameplayModule"), "HudModule", name => name != "GameplayScreenModule");

            FollowerRenameEVO screen = followers.Single(f => f.OldName == "GameplayScreenModule");
            FollowerRenameEVO test = followers.Single(f => f.OldName == "GameplayScreenTestModule");

            Assert.IsFalse(screen.Follows);
            Assert.AreEqual("GameplayScreenModule", screen.NewName);
            Assert.IsFalse(test.Follows);
            Assert.AreEqual("GameplayScreenModule", test.Reason.Split(' ').Last().TrimEnd('.'), "the reason names the parent that keeps its name");
        }

        [Test]
        public void Renaming_a_screen_module_directly_carries_its_test_module()
        {
            List<FollowerRenameEVO> followers = new RenameFollowers().Of(Picked(Gameplay(), "GameplayScreenModule"), "HudScreenModule", _ => true);

            Assert.AreEqual(1, followers.Count);
            Assert.AreEqual("HudScreenTestModule", followers[0].NewName);
        }

        [Test]
        public void The_order_is_the_tree_order_so_a_parent_comes_before_what_it_holds()
        {
            List<FollowerRenameEVO> followers = new RenameFollowers().Of(Picked(Gameplay(), "GameplayModule"), "HudModule", _ => true);

            Assert.AreEqual("GameplayScreenModule", followers[0].OldName);
            Assert.AreEqual("GameplayScreenTestModule", followers[1].OldName);
        }
    }
}
