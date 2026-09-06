using System;
using System.Collections.Generic;
using System.Reflection;
using FlowIoC.BaseModule.Signals;
using FlowIoC.Editor.ModuleCards;
using FlowIoC.Editor.Modules;
using FlowIoC.Editor.ModuleScanner;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class FactsFixtureSignals : ISignalHolder
    {
        public FactsFixtureIncoming Incoming = new FactsFixtureIncoming();
        public FactsFixtureOutgoing Outgoing = new FactsFixtureOutgoing();
    }

    public class FactsFixtureIncoming
    {
        public Signal InitializePlayer = new Signal();
        public Signal<double> AddCurrency = new Signal<double>();
    }

    public class FactsFixtureOutgoing
    {
        public Signal<double> CurrencyChanged = new Signal<double>();
    }

    public class ModuleFactsCollectorTests
    {
        private static readonly Assembly Self = typeof(ModuleFactsCollectorTests).Assembly;

        private static ModuleFactsCollector Collector() => new ModuleFactsCollector(_ => Self);

        private static ModuleTargetEVO Target()
        {
            return new ModuleTargetEVO
            {
                Name = "PlayerModule",
                Kind = ModuleKind.Main,
                ExpectedAssemblyName = "Modules.Player",
            };
        }

        [Test]
        public void A_module_whose_own_assembly_is_not_loaded_collects_nothing()
        {
            var collector = new ModuleFactsCollector(_ => null);

            Assert.IsNull(collector.Collect(Target(), new List<ScannedModule>()));
        }

        [Test]
        public void The_incoming_signals_are_read_from_the_public_holder()
        {
            ModuleFactsEVO facts = Collector().Collect(Target(), new List<ScannedModule>());

            CollectionAssert.Contains(facts.Incoming, "AddCurrency(double)");
            CollectionAssert.Contains(facts.Incoming, "InitializePlayer()");
        }

        [Test]
        public void The_outgoing_signals_are_read_from_the_public_holder()
        {
            ModuleFactsEVO facts = Collector().Collect(Target(), new List<ScannedModule>());

            CollectionAssert.Contains(facts.Outgoing, "CurrencyChanged(double)");
        }

        [Test]
        public void The_signals_are_sorted_so_the_hash_does_not_turn_over()
        {
            var incoming = new List<string>(Collector().Collect(Target(), new List<ScannedModule>()).Incoming);
            var sorted = new List<string>(incoming);
            sorted.Sort(StringComparer.Ordinal);

            CollectionAssert.AreEqual(sorted, incoming);
        }

        [Test]
        public void The_children_are_listed_with_their_kind()
        {
            var children = new List<ScannedModule>
            {
                new ScannedModule {Name = "PlayerScreenModule", Kind = ModuleKind.Screen},
                new ScannedModule {Name = "PlayerTestModule", Kind = ModuleKind.Test},
            };

            ModuleFactsEVO facts = Collector().Collect(Target(), children);

            CollectionAssert.Contains(facts.SubModules, "PlayerScreenModule (Screen)");
            CollectionAssert.Contains(facts.SubModules, "PlayerTestModule (Test)");
        }

        [Test]
        public void The_kind_is_the_targets_kind()
        {
            Assert.AreEqual("Main", Collector().Collect(Target(), new List<ScannedModule>()).Kind);
        }
    }
}
