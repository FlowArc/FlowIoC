using FlowIoC.Editor.AgentRules;
using FlowIoC.Editor.ModuleCards;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class ModuleDirectoryIgnoreRuleTests
    {
        private const string EXISTING = "/[Ll]ibrary/*\n/[Tt]emp/\n";

        [Test]
        public void An_untouched_gitignore_gains_the_block()
        {
            BlockWriteResult result = new ModuleDirectoryIgnoreRule().Apply(EXISTING);

            Assert.AreEqual(BlockWriteStatus.Created, result.Status);
            StringAssert.Contains("MODULES.md", result.Text);
        }

        [Test]
        public void What_the_project_already_ignores_is_left_alone()
        {
            BlockWriteResult result = new ModuleDirectoryIgnoreRule().Apply(EXISTING);

            StringAssert.Contains("/[Ll]ibrary/*", result.Text);
            StringAssert.Contains("/[Tt]emp/", result.Text);
        }

        [Test]
        public void Applying_it_twice_changes_nothing()
        {
            var rule = new ModuleDirectoryIgnoreRule();

            string once = rule.Apply(EXISTING).Text;

            Assert.AreEqual(BlockWriteStatus.Unchanged, rule.Apply(once).Status);
        }

        [Test]
        public void A_gitignore_that_already_carries_the_rule_reports_it_present()
        {
            var rule = new ModuleDirectoryIgnoreRule();

            Assert.IsTrue(rule.IsPresent(rule.Apply(EXISTING).Text));
        }

        [Test]
        public void A_gitignore_without_the_rule_reports_it_absent()
        {
            Assert.IsFalse(new ModuleDirectoryIgnoreRule().IsPresent(EXISTING));
        }

        [Test]
        public void The_block_is_marked_with_hash_comments_so_git_ignores_the_markers()
        {
            BlockWriteResult result = new ModuleDirectoryIgnoreRule().Apply(EXISTING);

            StringAssert.Contains("# FLOWIOC:BEGIN", result.Text);
            StringAssert.Contains("# FLOWIOC:END", result.Text);
        }
    }
}
