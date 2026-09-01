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
            Assert.AreEqual(15, CreateLegacyPaths().AssetMoves.Count);
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
