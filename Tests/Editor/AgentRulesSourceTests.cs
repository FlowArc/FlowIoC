using System.IO;
using FlowIoC.Editor.AgentRules;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class AgentRulesSourceTests
    {
        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "FlowIoCAgentRulesSource_" + Path.GetRandomFileName());
            Directory.CreateDirectory(Path.Combine(_root, "Documentation~"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, true);
        }

        [Test]
        public void TryRead_returns_the_file_contents()
        {
            WriteRules("Never put an if in a Context.");

            bool ok = new AgentRulesSource(_root, "1.0.1").TryRead(out string body, out string error);

            Assert.IsTrue(ok, error);
            StringAssert.Contains("Never put an if in a Context.", body);
        }

        [Test]
        public void TryRead_substitutes_every_version_placeholder()
        {
            WriteRules("See https://example.com/blob/{VERSION}/a.md and /blob/{VERSION}/b.md");

            new AgentRulesSource(_root, "1.0.1").TryRead(out string body, out _);

            StringAssert.DoesNotContain("{VERSION}", body);
            StringAssert.Contains("/blob/1.0.1/a.md", body);
            StringAssert.Contains("/blob/1.0.1/b.md", body);
        }

        [Test]
        public void TryRead_fails_with_a_message_when_the_file_is_missing()
        {
            bool ok = new AgentRulesSource(_root, "1.0.1").TryRead(out string body, out string error);

            Assert.IsFalse(ok);
            Assert.IsNull(body);
            Assert.IsNotEmpty(error);
            StringAssert.Contains("AgentRules.md", error);
        }

        [Test]
        public void Version_is_the_value_it_was_constructed_with()
        {
            Assert.AreEqual("1.0.1", new AgentRulesSource(_root, "1.0.1").Version);
        }

        private void WriteRules(string text) =>
            File.WriteAllText(Path.Combine(_root, "Documentation~", "AgentRules.md"), text);
    }
}
