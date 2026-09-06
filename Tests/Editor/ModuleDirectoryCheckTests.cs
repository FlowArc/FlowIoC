using System.Collections.Generic;
using FlowIoC.Editor.ModuleCards;
using FlowIoC.Editor.Modules;
using FlowIoC.Editor.ModuleScanner;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class ModuleDirectoryCheckTests
    {
        private string _directory;
        private string _gitignore;

        private static ProjectTargetEVO Project() => new ProjectTargetEVO {ProjectRoot = "C:/project"};

        private static List<ModuleCardEntryEVO> Entries()
        {
            return new List<ModuleCardEntryEVO>
            {
                new ModuleCardEntryEVO
                {
                    Name = "PlayerModule",
                    Kind = ModuleKind.Main,
                    Group = "Assets/Modules",
                    RelativePath = "Assets/Modules/PlayerModule",
                    Depth = 0,
                    Purpose = "Owns the money.",
                    Concepts = "currency",
                },
            };
        }

        private ModuleDirectoryCheck Check(string directory, string gitignore)
        {
            _directory = directory;
            _gitignore = gitignore;

            return new ModuleDirectoryCheck(
                root => _directory,
                (root, text) => _directory = text,
                root => _gitignore,
                (root, text) => _gitignore = text,
                root => Entries());
        }

        [Test]
        public void A_project_with_no_directory_file_is_Fixable()
        {
            Assert.AreEqual(ModuleCheckStatus.Fixable, Check(null, "").Inspect(Project()).Status);
        }

        [Test]
        public void Fixing_writes_the_directory_and_the_ignore_rule()
        {
            ModuleDirectoryCheck check = Check(null, "/[Tt]emp/\n");

            check.Fix(Project());

            StringAssert.Contains("**PlayerModule**", _directory);
            StringAssert.Contains("MODULES.md", _gitignore);
            StringAssert.Contains("/[Tt]emp/", _gitignore);
        }

        [Test]
        public void A_current_directory_with_the_rule_in_place_is_Ok()
        {
            ModuleDirectoryCheck check = Check(null, "");
            check.Fix(Project());

            Assert.AreEqual(ModuleCheckStatus.Ok, check.Inspect(Project()).Status);
        }

        [Test]
        public void A_directory_that_no_longer_matches_the_cards_is_Fixable()
        {
            ModuleDirectoryCheck check = Check(null, "");
            check.Fix(Project());

            _directory = _directory.Replace("Owns the money.", "Owns something else.");

            Assert.AreEqual(ModuleCheckStatus.Fixable, check.Inspect(Project()).Status);
        }

        [Test]
        public void A_missing_ignore_rule_alone_is_Fixable()
        {
            ModuleDirectoryCheck check = Check(null, "");
            check.Fix(Project());

            _gitignore = "";

            FindingEVO finding = check.Inspect(Project());

            Assert.AreEqual(ModuleCheckStatus.Fixable, finding.Status);
            StringAssert.Contains("gitignore", finding.Message.ToLowerInvariant());
        }
    }
}
