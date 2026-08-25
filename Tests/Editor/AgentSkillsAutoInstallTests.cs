using System.IO;
using System.Linq;
using FlowIoC.Editor.AgentSkills;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class AgentSkillsAutoInstallTests
    {
        private string _package;
        private string _project;

        [SetUp]
        public void SetUp()
        {
            string temp = Path.Combine(Path.GetTempPath(), "FlowIoCAgentSkillsAuto_" + Path.GetRandomFileName());
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

        /// <summary>
        /// What a project installing FlowIoC gets: the skill is there without anyone opening a
        /// window, and the run says what it wrote so the folder is not a mystery.
        /// </summary>
        [Test]
        public void A_missing_skill_is_written_without_being_asked_for()
        {
            AgentSkillsInstallReport report = NewAutoInstall().Run();

            Assert.AreEqual("the first version", File.ReadAllText(Installed("SKILL.md")));
            CollectionAssert.AreEqual(new[] {"flowioc-data-types"}, report.Installed.ToList());
            CollectionAssert.IsEmpty(report.Failures.ToList());
        }

        /// <summary>
        /// Every session after the first. Nothing is written and nothing is reported, so the
        /// automatic install does not fill the console with news about a file that has not moved.
        /// </summary>
        [Test]
        public void A_skill_that_is_already_in_place_is_left_alone_and_not_reported()
        {
            NewAutoInstall().Run();

            AgentSkillsInstallReport second = NewAutoInstall().Run();

            CollectionAssert.IsEmpty(second.Installed.ToList());
            CollectionAssert.IsEmpty(second.Failures.ToList());
        }

        [Test]
        public void A_skill_the_package_has_since_changed_is_refreshed()
        {
            NewAutoInstall().Run();
            File.WriteAllText(Shipped("SKILL.md"), "the second version");

            AgentSkillsInstallReport report = NewAutoInstall().Run();

            Assert.AreEqual("the second version", File.ReadAllText(Installed("SKILL.md")));
            CollectionAssert.AreEqual(new[] {"flowioc-data-types"}, report.Installed.ToList());
        }

        /// <summary>
        /// The install runs unattended, so a package that ships no skills at all - or one whose
        /// folder cannot be read - has to come back as a report rather than as an exception in
        /// the middle of a domain reload.
        /// </summary>
        [Test]
        public void A_package_with_no_skills_folder_is_reported_rather_than_thrown()
        {
            Directory.Delete(Path.Combine(_package, "Documentation~", "Skills"), true);

            AgentSkillsInstallReport report = NewAutoInstall().Run();

            CollectionAssert.IsEmpty(report.Installed.ToList());
            Assert.AreEqual(1, report.Failures.Count);
            StringAssert.Contains("Skills", report.Failures[0]);
        }

        /// <summary>A consumer who writes their own skill keeps it: only shipped names are touched.</summary>
        [Test]
        public void A_skill_the_consumer_wrote_is_never_touched()
        {
            string mine = Path.Combine(_project, ".claude", "skills", "my-own-skill");
            Directory.CreateDirectory(mine);
            File.WriteAllText(Path.Combine(mine, "SKILL.md"), "mine");

            AgentSkillsInstallReport report = NewAutoInstall().Run();

            Assert.AreEqual("mine", File.ReadAllText(Path.Combine(mine, "SKILL.md")));
            CollectionAssert.AreEqual(new[] {"flowioc-data-types"}, report.Installed.ToList());
        }

        private AgentSkillsAutoInstall NewAutoInstall() =>
            new AgentSkillsAutoInstall(_project, new AgentSkillsSource(_package));

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
