using System.IO;
using FlowIoC.Editor.AgentRules;
using FlowIoC.Editor.AgentSkills;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class AgentSkillsRemovalWatcherTests
    {
        private string _package;
        private string _project;

        [SetUp]
        public void SetUp()
        {
            string temp = Path.Combine(Path.GetTempPath(), "FlowIoCAgentSkillsRemoval_" + Path.GetRandomFileName());
            _package = Path.Combine(temp, "package");
            _project = Path.Combine(temp, "project");

            Directory.CreateDirectory(_project);
            WriteShippedSkill("flowioc-data-types");

            new AgentSkillsInstaller(_project, new AgentSkillsSource(_package)).Install();
        }

        [TearDown]
        public void TearDown()
        {
            string temp = Directory.GetParent(_project)?.FullName;

            if (temp != null && Directory.Exists(temp))
                Directory.Delete(temp, true);
        }

        [Test]
        public void HandleRemoval_takes_the_shipped_skills_out_when_FlowIoC_is_removed()
        {
            SyncFileState[] states = NewWatcher().HandleRemoval(
                new[] {"com.unity.addressables", AgentSkillsRemovalWatcher.PackageName});

            Assert.IsFalse(Directory.Exists(Installed()));
            Assert.AreEqual(1, states.Length);
            Assert.AreEqual(SyncStatus.Absent, states[0].Status);
        }

        /// <summary>The uninstall leaves no empty shell of its own behind either.</summary>
        [Test]
        public void HandleRemoval_takes_the_skills_folder_with_it_when_nothing_else_is_in_there()
        {
            NewWatcher().HandleRemoval(new[] {AgentSkillsRemovalWatcher.PackageName});

            Assert.IsFalse(Directory.Exists(Path.Combine(_project, ".claude", "skills")));
            Assert.IsTrue(Directory.Exists(Path.Combine(_project, ".claude")));
        }

        /// <summary>
        /// The whole point of the file-by-file delete: a skill the consumer wrote is not ours to
        /// remove, and neither is the folder it lives in.
        /// </summary>
        [Test]
        public void HandleRemoval_leaves_a_skill_the_consumer_wrote_alone()
        {
            string mine = Path.Combine(_project, ".claude", "skills", "my-own-skill");
            Directory.CreateDirectory(mine);
            File.WriteAllText(Path.Combine(mine, "SKILL.md"), "mine");

            NewWatcher().HandleRemoval(new[] {AgentSkillsRemovalWatcher.PackageName});

            Assert.IsFalse(Directory.Exists(Installed()));
            Assert.AreEqual("mine", File.ReadAllText(Path.Combine(mine, "SKILL.md")));
        }

        /// <summary>
        /// A note left inside a shipped skill keeps that folder alive. The shipped file goes,
        /// the note stays: deleting it would be deleting work the package never wrote.
        /// </summary>
        [Test]
        public void HandleRemoval_keeps_a_folder_that_still_holds_something_of_the_consumers()
        {
            File.WriteAllText(Path.Combine(Installed(), "notes.md"), "mine");

            NewWatcher().HandleRemoval(new[] {AgentSkillsRemovalWatcher.PackageName});

            Assert.IsFalse(File.Exists(Path.Combine(Installed(), "SKILL.md")));
            Assert.AreEqual("mine", File.ReadAllText(Path.Combine(Installed(), "notes.md")));
        }

        [Test]
        public void HandleRemoval_ignores_the_removal_of_another_package()
        {
            SyncFileState[] states = NewWatcher().HandleRemoval(new[] {"com.unity.addressables"});

            Assert.IsTrue(File.Exists(Path.Combine(Installed(), "SKILL.md")));
            CollectionAssert.IsEmpty(states);
        }

        [Test]
        public void HandleRemoval_ignores_an_empty_list()
        {
            NewWatcher().HandleRemoval(new string[0]);

            Assert.IsTrue(File.Exists(Path.Combine(Installed(), "SKILL.md")));
        }

        [Test]
        public void HandleRemoval_ignores_a_null_list()
        {
            NewWatcher().HandleRemoval(null);

            Assert.IsTrue(File.Exists(Path.Combine(Installed(), "SKILL.md")));
        }

        /// <summary>
        /// A project that never had the skills installed - the consumer deleted them, or the
        /// package was added and removed inside one session - is nothing to report and nothing
        /// to fail over.
        /// </summary>
        [Test]
        public void HandleRemoval_reports_nothing_when_there_is_nothing_to_remove()
        {
            Directory.Delete(Installed(), true);

            SyncFileState[] states = NewWatcher().HandleRemoval(new[] {AgentSkillsRemovalWatcher.PackageName});

            CollectionAssert.IsEmpty(states);
        }

        private AgentSkillsRemovalWatcher NewWatcher() =>
            new AgentSkillsRemovalWatcher(_project, new AgentSkillsSource(_package));

        private string Installed() =>
            Path.Combine(_project, ".claude", "skills", "flowioc-data-types");

        private void WriteShippedSkill(string name)
        {
            string folder = Path.Combine(_package, "Documentation~", "Skills", name);
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "SKILL.md"), "the shipped text");
        }
    }
}
