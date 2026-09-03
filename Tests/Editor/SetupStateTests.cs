using System.IO;
using FlowIoC.Editor.SetupModules;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class SetupStateTests
    {
        private string _root;
        private SetupState _state;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "FlowIoCSetupState_" + Path.GetRandomFileName());
            Directory.CreateDirectory(Path.Combine(_root, "ProjectSettings"));
            _state = new SetupState(_root);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, true);
        }

        private string MarkerPath => Path.Combine(_root, "ProjectSettings", SetupState.FileName);

        [Test]
        public void A_project_with_no_marker_has_not_been_installed()
        {
            Assert.IsFalse(_state.IsInstalled());
        }

        [Test]
        public void MarkInstalled_writes_a_marker_that_reads_back_as_installed()
        {
            _state.MarkInstalled("1.3.0");

            Assert.IsTrue(File.Exists(MarkerPath));
            Assert.IsTrue(new SetupState(_root).IsInstalled());
        }

        [Test]
        public void The_marker_records_the_version_it_was_installed_at()
        {
            _state.MarkInstalled("1.3.0");

            StringAssert.Contains("1.3.0", File.ReadAllText(MarkerPath));
        }

        [Test]
        public void A_marker_that_cannot_be_parsed_reads_as_not_installed()
        {
            File.WriteAllText(MarkerPath, "this is not json");

            Assert.IsFalse(_state.IsInstalled());
        }

        [Test]
        public void A_marker_that_says_false_reads_as_not_installed()
        {
            File.WriteAllText(MarkerPath, "{\"setupModulesInstalled\":false}");

            Assert.IsFalse(_state.IsInstalled());
        }

        [Test]
        public void A_project_with_no_marker_names_no_installed_version()
        {
            Assert.AreEqual(string.Empty, _state.InstalledVersion());
        }

        [Test]
        public void InstalledVersion_reads_back_the_version_the_marker_was_written_at()
        {
            _state.MarkInstalled("1.3.0");

            Assert.AreEqual("1.3.0", new SetupState(_root).InstalledVersion());
        }

        [Test]
        public void A_marker_that_cannot_be_parsed_names_no_installed_version()
        {
            File.WriteAllText(MarkerPath, "this is not json");

            Assert.AreEqual(string.Empty, _state.InstalledVersion());
        }

        [Test]
        public void MarkInstalled_creates_the_ProjectSettings_folder_when_it_is_missing()
        {
            Directory.Delete(Path.Combine(_root, "ProjectSettings"), true);

            _state.MarkInstalled("1.3.0");

            Assert.IsTrue(File.Exists(MarkerPath));
        }
    }
}