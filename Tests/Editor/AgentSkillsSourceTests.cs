using System.IO;
using FlowIoC.Editor.AgentSkills;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class AgentSkillsSourceTests
    {
        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "FlowIoCAgentSkillsSource_" + Path.GetRandomFileName());
            Directory.CreateDirectory(SkillsFolder);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, true);
        }

        [Test]
        public void TryList_returns_every_folder_that_holds_a_manifest()
        {
            WriteSkill("first-skill");
            WriteSkill("second-skill");

            bool ok = new AgentSkillsSource(_root).TryList(out string[] skills, out string error);

            Assert.IsTrue(ok, error);
            Assert.AreEqual(2, skills.Length);
            Assert.AreEqual("first-skill", Path.GetFileName(skills[0]));
            Assert.AreEqual("second-skill", Path.GetFileName(skills[1]));
        }

        /// <summary>
        /// A folder without a SKILL.md is not a skill. Skipping it rather than reporting it lets
        /// shared material live beside the skills without turning into a broken entry.
        /// </summary>
        [Test]
        public void TryList_skips_a_folder_with_no_manifest()
        {
            WriteSkill("real-skill");
            Directory.CreateDirectory(Path.Combine(SkillsFolder, "not-a-skill"));

            new AgentSkillsSource(_root).TryList(out string[] skills, out _);

            Assert.AreEqual(1, skills.Length);
            Assert.AreEqual("real-skill", Path.GetFileName(skills[0]));
        }

        [Test]
        public void TryList_returns_an_empty_list_when_the_folder_holds_nothing()
        {
            bool ok = new AgentSkillsSource(_root).TryList(out string[] skills, out string error);

            Assert.IsTrue(ok, error);
            Assert.IsEmpty(skills);
        }

        [Test]
        public void TryList_fails_with_a_message_when_the_folder_is_missing()
        {
            Directory.Delete(SkillsFolder, true);

            bool ok = new AgentSkillsSource(_root).TryList(out string[] skills, out string error);

            Assert.IsFalse(ok);
            Assert.IsEmpty(skills);
            StringAssert.Contains("Skills", error);
        }

        private string SkillsFolder => Path.Combine(_root, "Documentation~", "Skills");

        private void WriteSkill(string name)
        {
            string folder = Path.Combine(SkillsFolder, name);
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "SKILL.md"), "---\nname: " + name + "\n---\n");
        }
    }
}
