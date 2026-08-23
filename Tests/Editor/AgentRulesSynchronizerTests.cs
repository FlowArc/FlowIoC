using System.IO;
using System.Linq;
using FlowIoC.Editor.AgentRules;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class AgentRulesSynchronizerTests
    {
        private string _projectRoot;
        private string _packageRoot;

        [SetUp]
        public void SetUp()
        {
            string temp = Path.Combine(Path.GetTempPath(), "FlowIoCAgentRulesSync_" + Path.GetRandomFileName());
            _projectRoot = Path.Combine(temp, "Project");
            _packageRoot = Path.Combine(temp, "Package");

            Directory.CreateDirectory(_projectRoot);
            Directory.CreateDirectory(Path.Combine(_packageRoot, "Documentation~"));
            WriteRules("A Context declares bindings and contains no if.");
        }

        [TearDown]
        public void TearDown()
        {
            var temp = Directory.GetParent(_projectRoot);
            if (temp != null && Directory.Exists(temp.FullName))
                Directory.Delete(temp.FullName, true);
        }

        [Test]
        public void Sync_creates_both_files_when_the_project_has_neither()
        {
            NewSynchronizer().Sync();

            StringAssert.Contains("contains no if", Read("AGENTS.md"));
            StringAssert.Contains("@AGENTS.md", Read("CLAUDE.md"));
        }

        [Test]
        public void Sync_keeps_rules_the_consumer_wrote_themselves()
        {
            Write("AGENTS.md", "# House rules\n\nUse tabs.\n");

            NewSynchronizer().Sync();

            StringAssert.Contains("Use tabs.", Read("AGENTS.md"));
            StringAssert.Contains("contains no if", Read("AGENTS.md"));
        }

        [Test]
        public void Sync_leaves_a_claude_file_alone_when_it_already_imports_agents()
        {
            const string existing = "# Project\n\n@AGENTS.md\n";
            Write("CLAUDE.md", existing);

            NewSynchronizer().Sync();

            Assert.AreEqual(existing, Read("CLAUDE.md"));
        }

        [Test]
        public void Sync_adds_the_import_to_a_claude_file_that_lacks_it()
        {
            Write("CLAUDE.md", "# Project\n\nBuild with make.\n");

            NewSynchronizer().Sync();

            StringAssert.Contains("@AGENTS.md", Read("CLAUDE.md"));
            StringAssert.Contains("Build with make.", Read("CLAUDE.md"));
        }

        [Test]
        public void Sync_is_idempotent_on_disk()
        {
            var synchronizer = NewSynchronizer();
            synchronizer.Sync();
            string first = Read("AGENTS.md");

            synchronizer.Sync();

            Assert.AreEqual(first, Read("AGENTS.md"));
        }

        [Test]
        public void Inspect_reports_absent_before_the_first_sync()
        {
            var state = NewSynchronizer().Inspect().First(s => s.Path.EndsWith("AGENTS.md"));

            Assert.AreEqual(SyncStatus.Absent, state.Status);
        }

        [Test]
        public void Inspect_writes_nothing()
        {
            NewSynchronizer().Inspect();

            Assert.IsFalse(File.Exists(Path.Combine(_projectRoot, "AGENTS.md")));
            Assert.IsFalse(File.Exists(Path.Combine(_projectRoot, "CLAUDE.md")));
        }

        [Test]
        public void Inspect_reports_current_after_a_sync()
        {
            var synchronizer = NewSynchronizer();
            synchronizer.Sync();

            var state = synchronizer.Inspect().First(s => s.Path.EndsWith("AGENTS.md"));

            Assert.AreEqual(SyncStatus.Current, state.Status);
        }

        [Test]
        public void Inspect_reports_stale_when_the_rule_text_changes()
        {
            NewSynchronizer().Sync();

            WriteRules("A Command holds no state.");

            var state = NewSynchronizer().Inspect().First(s => s.Path.EndsWith("AGENTS.md"));

            Assert.AreEqual(SyncStatus.Stale, state.Status);
        }

        [Test]
        public void Sync_refuses_a_malformed_block_and_leaves_the_file_untouched()
        {
            const string broken = "<!-- FLOWIOC:BEGIN version=1.0.0 hash=deadbeef -->\nhalf a block\n";
            Write("AGENTS.md", broken);

            var state = NewSynchronizer().Sync().First(s => s.Path.EndsWith("AGENTS.md"));

            Assert.AreEqual(SyncStatus.Malformed, state.Status);
            Assert.AreEqual(broken, Read("AGENTS.md"));
        }

        [Test]
        public void RemoveBlocks_strips_the_block_and_keeps_consumer_content()
        {
            Write("AGENTS.md", "# House rules\n\nUse tabs.\n");
            var synchronizer = NewSynchronizer();
            synchronizer.Sync();

            synchronizer.RemoveBlocks();

            StringAssert.Contains("Use tabs.", Read("AGENTS.md"));
            StringAssert.DoesNotContain("FLOWIOC:BEGIN", Read("AGENTS.md"));
        }

        [Test]
        public void Sync_reports_failure_when_the_rule_text_is_missing()
        {
            File.Delete(Path.Combine(_packageRoot, "Documentation~", "AgentRules.md"));

            var states = NewSynchronizer().Sync();

            Assert.AreEqual(SyncStatus.Failed, states.Single().Status);
            Assert.IsFalse(File.Exists(Path.Combine(_projectRoot, "AGENTS.md")));
        }

        private AgentRulesSynchronizer NewSynchronizer() =>
            new AgentRulesSynchronizer(_projectRoot, new AgentRulesSource(_packageRoot, "1.0.1"));

        private string Read(string name) => File.ReadAllText(Path.Combine(_projectRoot, name));

        private void Write(string name, string text) => File.WriteAllText(Path.Combine(_projectRoot, name), text);

        private void WriteRules(string text) =>
            File.WriteAllText(Path.Combine(_packageRoot, "Documentation~", "AgentRules.md"), text);
    }
}
