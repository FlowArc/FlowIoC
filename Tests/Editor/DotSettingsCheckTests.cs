using System.Collections.Generic;
using FlowIoC.Editor.ModuleScanner;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class DotSettingsCheckTests
    {
        private class FakeFile : DotSettingsFile
        {
            internal readonly List<string> Written = new List<string>();
            internal bool MatchesAnswer = true;
            internal IReadOnlyList<string> LastSkipFolders;

            internal override bool Matches(string path, IReadOnlyList<string> skipFolders)
            {
                LastSkipFolders = skipFolders;

                return MatchesAnswer;
            }

            internal override void Write(string path, IReadOnlyList<string> skipFolders)
            {
                LastSkipFolders = skipFolders;
                Written.Add(path);
            }
        }

        private static ModuleTargetEVO Target() => new ModuleTargetEVO
        {
            Name = "PlayerModule",
            AbsolutePath = "C:/proj/Assets/Modules/PlayerModule",
            ProjectRoot = "C:/proj",
            ExpectedAssemblyName = "Modules.Player",
            Layout = TestModuleLayout.With(
                TestModuleLayout.Folder("Scripts", isMandatory: true, isNamespaceProvider: false))
        };

        private static DotSettingsCheck Check(FakeFile file, params string[] settingsPaths) =>
            new DotSettingsCheck(
                new DotSettingsPlan(),
                file,
                module => settingsPaths,
                module => "C:/proj/Assets/Modules");

        [Test]
        public void Settings_files_that_match_the_plan_are_Ok()
        {
            var file = new FakeFile {MatchesAnswer = true};

            Assert.AreEqual(
                ModuleCheckStatus.Ok,
                Check(file, "C:/proj/Modules.Player.csproj.DotSettings").Inspect(Target()).Status);
        }

        [Test]
        public void A_settings_file_that_has_drifted_is_Fixable_and_named()
        {
            var file = new FakeFile {MatchesAnswer = false};

            FindingEVO finding = Check(file, "C:/proj/Modules.Player.csproj.DotSettings").Inspect(Target());

            Assert.AreEqual(ModuleCheckStatus.Fixable, finding.Status);
            StringAssert.Contains("Modules.Player.csproj.DotSettings", finding.Message);
        }

        /// <summary>
        /// A .csproj.DotSettings only applies to the project it is named after, so the Shared
        /// assembly needs its own file carrying the same entries. Writing only the module's would
        /// leave Rider computing Shared namespaces off the folder tree.
        /// </summary>
        [Test]
        public void Fix_writes_every_settings_file_the_module_owns()
        {
            var file = new FakeFile {MatchesAnswer = false};

            Check(file,
                    "C:/proj/Modules.Player.csproj.DotSettings",
                    "C:/proj/Modules.Player.Shared.csproj.DotSettings")
                .Fix(Target());

            CollectionAssert.AreEqual(
                new[]
                {
                    "C:/proj/Modules.Player.csproj.DotSettings",
                    "C:/proj/Modules.Player.Shared.csproj.DotSettings"
                },
                file.Written);
        }

        [Test]
        public void A_module_with_no_assembly_yet_owns_no_settings_file_and_is_Ok()
        {
            var file = new FakeFile {MatchesAnswer = false};

            Assert.AreEqual(ModuleCheckStatus.Ok, Check(file).Inspect(Target()).Status);
        }

        /// <summary>
        /// The file is compared against the plan, not against itself: the entries the check asks
        /// about have to be the ones DotSettingsPlan computed for this module.
        /// </summary>
        [Test]
        public void The_file_is_asked_about_the_folders_the_plan_named()
        {
            var file = new FakeFile {MatchesAnswer = true};

            Check(file, "C:/proj/Modules.Player.csproj.DotSettings").Inspect(Target());

            CollectionAssert.AreEqual(
                new[] {"C:/proj/Assets/Modules/PlayerModule/Scripts"},
                Normalized(file.LastSkipFolders));
        }

        private static List<string> Normalized(IReadOnlyList<string> paths)
        {
            var normalized = new List<string>();

            foreach (string path in paths)
                normalized.Add(path.Replace('\\', '/'));

            return normalized;
        }
    }
}
