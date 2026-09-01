using FlowIoC.Editor.AgentRules;
using FlowIoC.Editor.Modules;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The ignore rule shares its block machinery with the AGENTS.md rules, so what is worth
    /// testing here is the part that differs: a comment syntax with no closing delimiter, and the
    /// promise that a user's own lines in the same file survive a rewrite.
    /// </summary>
    public class ModuleIndexIgnoreRuleTests
    {
        private ModuleIndexIgnoreRule _rule;
        private ManagedBlockWriter _writer;

        [SetUp]
        public void SetUp()
        {
            _rule = new ModuleIndexIgnoreRule();
            _writer = new ManagedBlockWriter(_rule.Style);
        }

        [Test]
        public void The_body_names_the_index_asset_and_its_meta()
        {
            Assert.AreEqual("ED_ModuleIndex.asset\nED_ModuleIndex.asset.meta", _rule.Body());
        }

        [Test]
        public void Every_line_of_the_written_block_is_a_comment_or_a_pattern()
        {
            string text = _writer.Write(string.Empty, _rule.Body(), "1").Text;

            foreach (string line in text.Split('\n'))
            {
                string trimmed = line.Trim();
                if (trimmed.Length == 0) continue;

                bool isComment = trimmed.StartsWith("#");
                bool isPattern = trimmed.StartsWith("ED_ModuleIndex.asset");

                Assert.IsTrue(isComment || isPattern, $"'{trimmed}' is neither a comment nor a pattern");
            }
        }

        [Test]
        public void The_markers_carry_no_closing_delimiter()
        {
            string text = _writer.Write(string.Empty, _rule.Body(), "1").Text;

            Assert.IsFalse(text.Contains("-->"), "the markdown closing delimiter leaked into a .gitignore");
            StringAssert.Contains("# FLOWIOC:BEGIN ", text);
            StringAssert.Contains("# FLOWIOC:END", text);
        }

        [Test]
        public void A_projects_own_lines_survive_a_rewrite()
        {
            const string mine = "*.log\nSecrets/\n";

            string created = _writer.Write(mine, _rule.Body(), "1").Text;
            string rewritten = _writer.Write(created, "SomethingElse.asset", "2").Text;

            StringAssert.Contains("*.log", rewritten);
            StringAssert.Contains("Secrets/", rewritten);
            StringAssert.Contains("SomethingElse.asset", rewritten);
            Assert.IsFalse(rewritten.Contains("ED_ModuleIndex.asset"), "the old block body was left behind");
        }

        [Test]
        public void Writing_the_same_block_twice_changes_nothing()
        {
            string created = _writer.Write("*.log\n", _rule.Body(), "1").Text;
            BlockWriteResult again = _writer.Write(created, _rule.Body(), "1");

            Assert.AreEqual(BlockWriteStatus.Unchanged, again.Status);
            Assert.AreEqual(created, again.Text);
        }

        [Test]
        public void A_file_with_a_half_written_block_is_refused_rather_than_rewritten()
        {
            const string broken = "*.log\n# FLOWIOC:END\n";

            BlockWriteResult result = _writer.Write(broken, _rule.Body(), "1");

            Assert.AreEqual(BlockWriteStatus.Refused, result.Status);
            Assert.AreEqual(broken, result.Text);
            StringAssert.Contains(".gitignore", result.Message);
        }
    }
}
