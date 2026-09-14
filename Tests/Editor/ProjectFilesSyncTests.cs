using FlowIoC.Editor.ProjectFiles;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The project files are regenerated the first time a FlowIoC version runs in a project on
    /// this machine, and never again for that version - the fix for an IDE left on the package's
    /// old folder after an update, pressed on the reader's behalf.
    /// </summary>
    public class ProjectFilesSyncTests
    {
        private class RecordingRegenerator : IProjectFilesRegenerator
        {
            public int Regenerated;
            public string EditorName => "RecordingEditor";
            public void Regenerate() => Regenerated++;
        }

        private const string ROOT = "C:/flowioc-tests/project-files";

        private readonly ProjectFilesSyncRule _rule = new();
        private ProjectFilesSyncedVersion _synced;

        [SetUp]
        public void SetUp()
        {
            _synced = new ProjectFilesSyncedVersion(ROOT);
            _synced.Forget();
        }

        [TearDown]
        public void TearDown() => _synced.Forget();

        [Test]
        public void A_version_never_synced_is_regenerated()
        {
            Assert.IsTrue(_rule.ShouldRegenerate("1.15.2", string.Empty));
        }

        [Test]
        public void A_version_already_synced_is_left_alone()
        {
            Assert.IsFalse(_rule.ShouldRegenerate("1.15.2", "1.15.2"));
        }

        [Test]
        public void An_update_is_regenerated_once_more()
        {
            Assert.IsTrue(_rule.ShouldRegenerate("1.15.2", "1.15.1"));
        }

        /// <summary>An embedded copy outside the Package Manager has no version, and nothing to record.</summary>
        [Test]
        public void No_version_is_nothing_to_do()
        {
            Assert.IsFalse(_rule.ShouldRegenerate(string.Empty, string.Empty));
            Assert.IsFalse(_rule.ShouldRegenerate(null, "1.15.1"));
        }

        [Test]
        public void The_first_run_regenerates_and_records_the_version_so_the_next_run_does_not()
        {
            var regenerator = new RecordingRegenerator();
            var sync = new ProjectFilesStartupSync("1.15.2", _synced, regenerator, batchMode: false);

            sync.Run();
            sync.Run();

            Assert.AreEqual(1, regenerator.Regenerated);
            Assert.AreEqual("1.15.2", _synced.Read());
        }

        [Test]
        public void An_update_regenerates_again_and_moves_the_record_on()
        {
            var regenerator = new RecordingRegenerator();
            new ProjectFilesStartupSync("1.15.1", _synced, regenerator, batchMode: false).Run();

            new ProjectFilesStartupSync("1.15.2", _synced, regenerator, batchMode: false).Run();

            Assert.AreEqual(2, regenerator.Regenerated);
            Assert.AreEqual("1.15.2", _synced.Read());
        }
    }
}
