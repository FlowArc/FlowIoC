using System.IO;
using FlowIoC.Editor.AgentRules;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class AgentRulesRemovalWatcherTests
    {
        private string _projectRoot;
        private string _packageRoot;

        [SetUp]
        public void SetUp()
        {
            string temp = Path.Combine(Path.GetTempPath(), "FlowIoCAgentRulesRemoval_" + Path.GetRandomFileName());
            _projectRoot = Path.Combine(temp, "Project");
            _packageRoot = Path.Combine(temp, "Package");

            Directory.CreateDirectory(_projectRoot);
            Directory.CreateDirectory(Path.Combine(_packageRoot, "Documentation~"));
            File.WriteAllText(
                Path.Combine(_packageRoot, "Documentation~", "AgentRules.md"),
                "A Context declares bindings and contains no if.");

            File.WriteAllText(Path.Combine(_projectRoot, "AGENTS.md"), "# House rules\n\nUse tabs.\n");
            NewSynchronizer().Sync();
        }

        [TearDown]
        public void TearDown()
        {
            var temp = Directory.GetParent(_projectRoot);
            if (temp != null && Directory.Exists(temp.FullName))
                Directory.Delete(temp.FullName, true);
        }

        [Test]
        public void HandleRemoval_strips_the_block_when_FlowIoC_is_removed()
        {
            NewWatcher().HandleRemoval(new[] { "com.unity.addressables", "com.flowioc.core" });

            StringAssert.DoesNotContain("FLOWIOC:BEGIN", Agents());
            StringAssert.Contains("Use tabs.", Agents());
        }

        [Test]
        public void HandleRemoval_ignores_the_removal_of_another_package()
        {
            NewWatcher().HandleRemoval(new[] { "com.unity.addressables" });

            StringAssert.Contains("FLOWIOC:BEGIN", Agents());
        }

        [Test]
        public void HandleRemoval_ignores_an_empty_list()
        {
            NewWatcher().HandleRemoval(new string[0]);

            StringAssert.Contains("FLOWIOC:BEGIN", Agents());
        }

        [Test]
        public void HandleRemoval_ignores_null()
        {
            NewWatcher().HandleRemoval(null);

            StringAssert.Contains("FLOWIOC:BEGIN", Agents());
        }

        private AgentRulesRemovalWatcher NewWatcher() =>
            new AgentRulesRemovalWatcher(_projectRoot, new AgentRulesSource(_packageRoot, "1.0.1"));

        private AgentRulesSynchronizer NewSynchronizer() =>
            new AgentRulesSynchronizer(_projectRoot, new AgentRulesSource(_packageRoot, "1.0.1"));

        private string Agents() => File.ReadAllText(Path.Combine(_projectRoot, "AGENTS.md"));
    }
}
