using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlowIoC.Editor.Modules;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class MarkerFileSweeperTests
    {
        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "FlowIoCSweep_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_root);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }

        private void WriteFile(string relativePath, string content = "x")
        {
            string full = Path.Combine(_root, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllText(full, content);
        }

        private bool Exists(string relativePath)
        {
            return File.Exists(Path.Combine(_root, relativePath));
        }

        [Test]
        public void The_module_marker_is_a_marker_file()
        {
            Assert.IsTrue(new MarkerFileSweeper().IsMarkerFile("_module_info.txt"));
        }

        [Test]
        public void A_folder_marker_is_a_marker_file()
        {
            Assert.IsTrue(new MarkerFileSweeper().IsMarkerFile("_controllers_info.txt"));
        }

        [Test]
        public void A_container_marker_is_a_marker_file()
        {
            Assert.IsTrue(new MarkerFileSweeper().IsMarkerFile("_screenmodules_info.txt"));
        }

        /// <summary>
        /// The sweeper deletes files. Anything that is not unmistakably one of ours has to
        /// survive, including a file the user happened to name something similar.
        /// </summary>
        [Test]
        public void A_file_the_project_owns_is_not_a_marker_file()
        {
            var sweeper = new MarkerFileSweeper();

            Assert.IsFalse(sweeper.IsMarkerFile("notes.txt"));
            Assert.IsFalse(sweeper.IsMarkerFile("module_info.txt"));
            Assert.IsFalse(sweeper.IsMarkerFile("_module_info.md"));
            Assert.IsFalse(sweeper.IsMarkerFile("_info.txt"));
            Assert.IsFalse(sweeper.IsMarkerFile("_module_info.txt.bak"));
            Assert.IsFalse(sweeper.IsMarkerFile("_Recipe_Info.TXT"));
        }

        [Test]
        public void Sweeping_deletes_the_marker_and_its_meta_together()
        {
            WriteFile("Modules/CameraModule/_module_info.txt");
            WriteFile("Modules/CameraModule/_module_info.txt.meta");

            new MarkerFileSweeper().Sweep(_root);

            Assert.IsFalse(Exists("Modules/CameraModule/_module_info.txt"));
            Assert.IsFalse(Exists("Modules/CameraModule/_module_info.txt.meta"));
        }

        /// <summary>
        /// Deleting the meta is conditional on the marker itself actually being gone. A locked
        /// marker must not lose its meta — that would orphan an asset with no meta file, which is
        /// worse than leaving the pair alone for the next sweep to retry.
        /// </summary>
        [Test]
        public void A_locked_marker_keeps_its_meta_rather_than_losing_it()
        {
            WriteFile("Modules/CameraModule/_module_info.txt");
            WriteFile("Modules/CameraModule/_module_info.txt.meta");

            string markerPath = Path.Combine(_root, "Modules/CameraModule/_module_info.txt");
            List<string> deleted;

            using (new FileStream(markerPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                deleted = new MarkerFileSweeper().Sweep(_root);
            }

            Assert.IsTrue(Exists("Modules/CameraModule/_module_info.txt"));
            Assert.IsTrue(Exists("Modules/CameraModule/_module_info.txt.meta"));
            Assert.IsEmpty(deleted);
        }

        [Test]
        public void Sweeping_reaches_every_depth_of_the_tree()
        {
            WriteFile("Modules/MainModule/zScreenModules/HudModule/Scripts/Runtime/Models/_models_info.txt");

            new MarkerFileSweeper().Sweep(_root);

            Assert.IsFalse(Exists("Modules/MainModule/zScreenModules/HudModule/Scripts/Runtime/Models/_models_info.txt"));
        }

        [Test]
        public void Sweeping_leaves_everything_else_alone()
        {
            WriteFile("Modules/CameraModule/_module_info.txt");
            WriteFile("Modules/CameraModule/Modules.Camera.asmdef");
            WriteFile("Modules/CameraModule/notes.txt");

            new MarkerFileSweeper().Sweep(_root);

            Assert.IsTrue(Exists("Modules/CameraModule/Modules.Camera.asmdef"));
            Assert.IsTrue(Exists("Modules/CameraModule/notes.txt"));
        }

        [Test]
        public void Sweeping_reports_what_it_deleted()
        {
            WriteFile("Modules/CameraModule/_module_info.txt");
            WriteFile("Modules/CameraModule/Scripts/Runtime/Models/_models_info.txt");

            List<string> deleted = new MarkerFileSweeper().Sweep(_root);

            Assert.AreEqual(2, deleted.Count(d => d.EndsWith(".txt")));
        }

        [Test]
        public void Sweeping_a_missing_root_reports_nothing_rather_than_throwing()
        {
            Assert.IsEmpty(new MarkerFileSweeper().Sweep(Path.Combine(_root, "Nope")));
        }
    }
}
