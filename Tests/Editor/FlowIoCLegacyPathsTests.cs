using System.Linq;
using FlowIoC.BaseModule.ProjectPaths;
using FlowIoC.Editor.Migration;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class FlowIoCLegacyPathsTests
    {
        private FlowIoCLegacyPaths CreateLegacyPaths()
        {
            return new FlowIoCLegacyPaths(new FlowIoCProjectPaths());
        }

        [Test]
        public void Every_project_local_asset_FlowIoC_ever_wrote_is_covered()
        {
            Assert.AreEqual(12, CreateLegacyPaths().AssetMoves.Count);
            Assert.AreEqual(7, CreateLegacyPaths().AssetsToDelete.Count);
        }

        /// <summary>
        /// The settings asset held nothing of its own once its four owners took their parts back,
        /// so every name it ever had is deleted rather than carried to a new home - and nothing
        /// asks to move a file to a path that no longer exists.
        /// </summary>
        [Test]
        public void The_console_settings_asset_is_deleted_under_every_name_it_had()
        {
            FlowIoCLegacyPaths legacy = CreateLegacyPaths();

            foreach (string retired in new[]
                     {
                         "Assets/Resources/FlowConsoleSettings.asset",
                         "Assets/Plugins/FlowIoC/Resources/FlowConsoleSettings.asset",
                         "Assets/Plugins/FlowIoC/Resources/CD_FlowConsole.asset"
                     })
            {
                Assert.IsTrue(legacy.AssetsToDelete.Contains(retired), retired + " is not deleted.");
                Assert.IsFalse(legacy.AssetMoves.Any(move => move.Legacy == retired), retired + " is still moved.");
            }

            Assert.IsTrue(legacy.FoldersToCleanUp.Contains("Assets/Plugins/FlowIoC/Resources"));
        }

        /// <summary>
        /// The shared FlowLogType file carried only Default, which the package declares itself now.
        /// It is deleted under both roots it ever had, with the asmref beside it, and never moved.
        /// </summary>
        [Test]
        public void The_shared_FlowLogType_file_is_deleted_under_every_root_it_had()
        {
            FlowIoCLegacyPaths legacy = CreateLegacyPaths();

            foreach (string retired in new[]
                     {
                         "Assets/FlowIoC/Generated/FlowLogType.cs",
                         "Assets/FlowIoC/Generated/FlowIoC.Generated.asmref",
                         "Assets/Plugins/FlowIoC/Generated/FlowLogType.cs",
                         "Assets/Plugins/FlowIoC/Generated/FlowIoC.Generated.asmref"
                     })
            {
                Assert.IsTrue(legacy.AssetsToDelete.Contains(retired), retired + " is not deleted.");
                Assert.IsFalse(legacy.AssetMoves.Any(move => move.Legacy == retired), retired + " is still moved.");
            }

            Assert.IsTrue(legacy.FoldersToCleanUp.Contains("Assets/Plugins/FlowIoC/Generated"));
        }

        [Test]
        public void Every_destination_lives_under_the_new_root()
        {
            string root = new FlowIoCProjectPaths().Root;

            foreach (LegacyAssetMove move in CreateLegacyPaths().AssetMoves)
            {
                Assert.That(move.Destination, Does.StartWith(root), move.Legacy);
            }
        }

        [Test]
        public void No_asset_is_asked_to_move_onto_itself()
        {
            foreach (LegacyAssetMove move in CreateLegacyPaths().AssetMoves)
            {
                Assert.AreNotEqual(move.Legacy, move.Destination);
            }
        }

        [Test]
        public void Every_legacy_path_is_listed_only_once()
        {
            var moves = CreateLegacyPaths().AssetMoves;

            Assert.AreEqual(moves.Count, moves.Select(move => move.Legacy).Distinct().Count());
        }

        /// <summary>
        /// A folder can only be deleted once it is empty, so a child folder has to be listed before
        /// any folder that contains it.
        /// </summary>
        [Test]
        public void Legacy_folders_are_listed_deepest_first()
        {
            var folders = CreateLegacyPaths().FoldersToCleanUp;

            for (int i = 0; i < folders.Count; i++)
            {
                for (int j = i + 1; j < folders.Count; j++)
                {
                    Assert.IsFalse(
                        folders[j].StartsWith(folders[i] + "/"),
                        $"{folders[j]} sits inside {folders[i]} but is listed after it.");
                }
            }
        }
    }
}