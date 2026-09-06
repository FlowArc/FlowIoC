using System.Collections.Generic;
using FlowIoC.Editor.ModuleCards;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class ModuleCardBodyBuilderTests
    {
        private static ModuleFactsEVO Facts()
        {
            return new ModuleFactsEVO
            {
                Kind = "Main",
                Assemblies = new List<string> {"Modules.Player", "Modules.Player.Shared", "Modules.Player.Signals"},
                RootType = "PlayerRoot",
                ContextType = "PlayerContext",
                Incoming = new List<string> {"AddCurrency(double)", "InitializePlayer()"},
                Outgoing = new List<string> {"CurrencyChanged(double)"},
                Publishes = new List<string> {"CD_PlayerRules", "PlayerStateVO"},
                Services = new List<string>(),
                Systems = new List<string> {"IPlayerSystem"},
                SubModules = new List<string> {"PlayerScreenModule (Screen)", "PlayerTestModule (Test)"},
            };
        }

        [Test]
        public void The_first_line_carries_the_kind_and_the_assemblies()
        {
            string body = new ModuleCardBodyBuilder().Build(Facts());

            StringAssert.StartsWith(
                "**Kind** Main · **Assemblies** Modules.Player, Modules.Player.Shared, Modules.Player.Signals",
                body);
        }

        [Test]
        public void A_module_with_a_Root_shows_the_Root_and_the_Context()
        {
            StringAssert.Contains("**Root** PlayerRoot → PlayerContext", new ModuleCardBodyBuilder().Build(Facts()));
        }

        [Test]
        public void A_module_with_no_Root_shows_the_Context_alone()
        {
            ModuleFactsEVO facts = Facts();
            facts.RootType = null;
            facts.ContextType = "PlayerScreenContext";

            string body = new ModuleCardBodyBuilder().Build(facts);

            StringAssert.Contains("**Context** PlayerScreenContext", body);
            StringAssert.DoesNotContain("**Root**", body);
        }

        [Test]
        public void The_signal_lines_carry_every_signal_with_its_parameters()
        {
            string body = new ModuleCardBodyBuilder().Build(Facts());

            StringAssert.Contains("**Incoming** AddCurrency(double) · InitializePlayer()", body);
            StringAssert.Contains("**Outgoing** CurrencyChanged(double)", body);
        }

        [Test]
        public void A_module_with_no_signal_holder_shows_neither_signal_line()
        {
            ModuleFactsEVO facts = Facts();
            facts.Incoming = new List<string>();
            facts.Outgoing = new List<string>();

            string body = new ModuleCardBodyBuilder().Build(facts);

            StringAssert.DoesNotContain("**Incoming**", body);
            StringAssert.DoesNotContain("**Outgoing**", body);
        }

        [Test]
        public void An_empty_list_prints_no_line_at_all()
        {
            StringAssert.DoesNotContain("**Services**", new ModuleCardBodyBuilder().Build(Facts()));
        }

        [Test]
        public void The_same_facts_always_render_the_same_text()
        {
            Assert.AreEqual(
                new ModuleCardBodyBuilder().Build(Facts()),
                new ModuleCardBodyBuilder().Build(Facts()));
        }
    }
}
