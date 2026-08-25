using System.IO;
using System.Text.RegularExpressions;
using FlowIoC.Editor.AgentSkills;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The skills this package actually ships, checked against what the installer and the
    /// assistant reading them assume. These run against the real Documentation~/Skills folder,
    /// so a new skill added without the shape the others have fails here rather than in a
    /// consumer project.
    /// </summary>
    public class ShippedSkillsTests
    {
        private string[] _skills;

        [SetUp]
        public void SetUp()
        {
            bool ok = new AgentSkillsSource().TryList(out _skills, out string error);

            Assert.IsTrue(ok, error);
        }

        [Test]
        public void The_package_ships_at_least_one_skill()
        {
            CollectionAssert.IsNotEmpty(_skills);
        }

        /// <summary>
        /// A skill folder is installed under its own name, and the assistant matches the folder
        /// against the name in the manifest. The two drifting apart is the kind of thing nobody
        /// notices until a skill silently fails to load.
        /// </summary>
        [Test]
        public void Every_skill_declares_the_name_of_the_folder_it_lives_in()
        {
            foreach (string skill in _skills)
            {
                string folder = Path.GetFileName(skill);
                Match name = Regex.Match(Manifest(skill), @"^name:\s*(\S+)\s*$", RegexOptions.Multiline);

                Assert.IsTrue(name.Success, $"'{folder}' has no name in its frontmatter.");
                Assert.AreEqual(folder, name.Groups[1].Value);
            }
        }

        /// <summary>
        /// The description is the whole of what an assistant reads before deciding to load a
        /// skill, so an empty one makes the skill unreachable.
        /// </summary>
        [Test]
        public void Every_skill_says_when_it_should_be_used()
        {
            foreach (string skill in _skills)
            {
                Match description = Regex.Match(Manifest(skill), @"^description:\s*(\S.*)$", RegexOptions.Multiline);

                Assert.IsTrue(description.Success, $"'{Path.GetFileName(skill)}' has no description.");
                StringAssert.StartsWith("Use when", description.Groups[1].Value);
            }
        }

        /// <summary>
        /// The removal watcher only fires for a Package Manager uninstall. A manifest edited by
        /// hand, or a package folder simply deleted, leaves the skill sitting in the project - so
        /// every skill has to be able to say for itself that it no longer applies, the same way
        /// the agent rules block does.
        /// </summary>
        [Test]
        public void Every_skill_says_it_only_applies_while_FlowIoC_is_installed()
        {
            foreach (string skill in _skills)
            {
                string text = File.ReadAllText(Path.Combine(skill, AgentSkillsSource.ManifestFileName));

                StringAssert.Contains("only while FlowIoC is installed", text);
                StringAssert.Contains("com.flowarc.flowioc.core", text);
            }
        }

        private string Manifest(string skillFolder) =>
            File.ReadAllText(Path.Combine(skillFolder, AgentSkillsSource.ManifestFileName));
    }
}
