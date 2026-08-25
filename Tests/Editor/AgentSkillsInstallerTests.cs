using System.IO;
using FlowIoC.Editor.AgentRules;
using FlowIoC.Editor.AgentSkills;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class AgentSkillsInstallerTests
    {
        private string _package;
        private string _project;

        [SetUp]
        public void SetUp()
        {
            string temp = Path.Combine(Path.GetTempPath(), "FlowIoCAgentSkills_" + Path.GetRandomFileName());
            _package = Path.Combine(temp, "package");
            _project = Path.Combine(temp, "project");

            Directory.CreateDirectory(_project);
            WriteShippedSkill("flowioc-data-types", "the first version");
        }

        [TearDown]
        public void TearDown()
        {
            string temp = Directory.GetParent(_project)?.FullName;

            if (temp != null && Directory.Exists(temp))
                Directory.Delete(temp, true);
        }

        [Test]
        public void Inspect_reports_a_skill_that_was_never_installed_as_absent()
        {
            SyncFileState[] states = NewInstaller().Inspect();

            Assert.AreEqual(1, states.Length);
            Assert.AreEqual(SyncStatus.Absent, states[0].Status);
            Assert.IsFalse(File.Exists(Installed("SKILL.md")));
        }

        [Test]
        public void Install_writes_the_skill_into_the_projects_claude_folder()
        {
            SyncFileState[] states = NewInstaller().Install();

            Assert.AreEqual(SyncStatus.Current, states[0].Status);
            StringAssert.Contains("the first version", File.ReadAllText(Installed("SKILL.md")));
        }

        [Test]
        public void Install_carries_supporting_files_along_with_the_manifest()
        {
            File.WriteAllText(Shipped("example.cs"), "class MapCVO { }");

            NewInstaller().Install();

            Assert.IsTrue(File.Exists(Installed("example.cs")));
        }

        [Test]
        public void A_skill_that_matches_what_the_package_ships_is_current()
        {
            NewInstaller().Install();

            Assert.AreEqual(SyncStatus.Current, NewInstaller().Inspect()[0].Status);
        }

        [Test]
        public void A_skill_the_package_has_since_changed_is_stale()
        {
            NewInstaller().Install();
            File.WriteAllText(Shipped("SKILL.md"), "the second version");

            Assert.AreEqual(SyncStatus.Stale, NewInstaller().Inspect()[0].Status);

            NewInstaller().Install();

            Assert.AreEqual("the second version", File.ReadAllText(Installed("SKILL.md")));
        }

        /// <summary>
        /// Only what the package owns is compared, so a file the consumer put beside the skill
        /// neither makes it stale nor gets removed by an install.
        /// </summary>
        [Test]
        public void A_file_the_consumer_added_is_left_alone()
        {
            NewInstaller().Install();
            File.WriteAllText(Installed("notes.md"), "mine");

            Assert.AreEqual(SyncStatus.Current, NewInstaller().Inspect()[0].Status);

            NewInstaller().Install();

            Assert.AreEqual("mine", File.ReadAllText(Installed("notes.md")));
        }

        /// <summary>The same text with the other line ending convention is not a change.</summary>
        [Test]
        public void Line_endings_alone_do_not_make_a_skill_stale()
        {
            File.WriteAllText(Shipped("SKILL.md"), "one\ntwo\n");
            NewInstaller().Install();
            File.WriteAllText(Installed("SKILL.md"), "one\r\ntwo\r\n");

            Assert.AreEqual(SyncStatus.Current, NewInstaller().Inspect()[0].Status);
        }

        private AgentSkillsInstaller NewInstaller() =>
            new AgentSkillsInstaller(_project, new AgentSkillsSource(_package));

        private string Shipped(string file) =>
            Path.Combine(_package, "Documentation~", "Skills", "flowioc-data-types", file);

        private string Installed(string file) =>
            Path.Combine(_project, ".claude", "skills", "flowioc-data-types", file);

        private void WriteShippedSkill(string name, string body)
        {
            string folder = Path.Combine(_package, "Documentation~", "Skills", name);
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "SKILL.md"), body);
        }
    }
}
