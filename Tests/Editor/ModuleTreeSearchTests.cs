using System.Collections.Generic;
using System.Linq;
using FlowIoC.Editor.CodeGenerator.Menus.Module;
using FlowIoC.Editor.Modules;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// A search over a module tree keeps a match with everything inside it, and keeps the modules
    /// above a match so the match is never shown without the module it lives in. What matches
    /// nothing and holds nothing that matches is the only thing that goes.
    /// </summary>
    public class ModuleTreeSearchTests
    {
        private static ModulePickEVO Pick(string name, string parent) =>
            new ModulePickEVO {Name = name, Kind = ModuleKind.Main, ParentName = parent, Path = "D:/p/" + name};

        private static List<ModuleTreeRowEVO<ModulePickEVO>> Tree() => new ModuleTree().Build(new List<ModulePickEVO>
        {
            Pick("GameplayModule", null),
            Pick("GameplayScreenModule", "GameplayModule"),
            Pick("GameplayScreenTestModule", "GameplayScreenModule"),
            Pick("MatchBoardScreenModule", "GameplayModule"),
            Pick("CounterModule", null),
            Pick("CounterTestModule", "CounterModule")
        });

        private static IEnumerable<string> Names(IEnumerable<ModuleTreeRowEVO<ModulePickEVO>> rows) => rows.Select(row => row.Row.Name);

        [Test]
        public void An_empty_search_keeps_every_row_in_order()
        {
            List<ModuleTreeRowEVO<ModulePickEVO>> tree = Tree();

            CollectionAssert.AreEqual(Names(tree), Names(new ModuleTreeSearch().Filter(tree, "")));
            CollectionAssert.AreEqual(Names(tree), Names(new ModuleTreeSearch().Filter(tree, null)));
        }

        [Test]
        public void A_match_brings_everything_inside_it()
        {
            CollectionAssert.AreEqual(
                new[] {"GameplayModule", "GameplayScreenModule", "GameplayScreenTestModule", "MatchBoardScreenModule"},
                Names(new ModuleTreeSearch().Filter(Tree(), "gameplaymodule")));
        }

        [Test]
        public void A_matched_descendant_keeps_the_modules_above_it_and_not_its_siblings()
        {
            CollectionAssert.AreEqual(
                new[] {"GameplayModule", "MatchBoardScreenModule"},
                Names(new ModuleTreeSearch().Filter(Tree(), "MatchBoard")));
        }

        [Test]
        public void The_search_is_a_case_insensitive_substring()
        {
            CollectionAssert.AreEqual(
                new[] {"CounterModule", "CounterTestModule", "GameplayModule", "GameplayScreenModule", "GameplayScreenTestModule"},
                Names(new ModuleTreeSearch().Filter(Tree(), "TEST")));
        }

        [Test]
        public void Nothing_matching_leaves_nothing()
        {
            Assert.IsEmpty(new ModuleTreeSearch().Filter(Tree(), "Nope"));
        }
    }
}
