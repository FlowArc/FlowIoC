using FlowIoC.Editor.Migration;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class LegacyAssetMovePolicyTests
    {
        [Test]
        public void An_asset_moves_when_the_legacy_copy_exists_and_the_destination_is_free()
        {
            Assert.IsTrue(new LegacyAssetMovePolicy()
                .ShouldMove(legacyExists: true, destinationExists: false));
        }

        /// <summary>
        /// The failure this guards against: overwriting a destination silently would throw away
        /// whichever copy holds the user's real log types or folder colors.
        /// </summary>
        [Test]
        public void Nothing_moves_when_the_destination_is_already_occupied()
        {
            Assert.IsFalse(new LegacyAssetMovePolicy()
                .ShouldMove(legacyExists: true, destinationExists: true));
        }

        [Test]
        public void Nothing_moves_in_an_already_migrated_project()
        {
            Assert.IsFalse(new LegacyAssetMovePolicy()
                .ShouldMove(legacyExists: false, destinationExists: true));
        }

        [Test]
        public void Nothing_moves_in_a_project_that_never_had_the_asset()
        {
            Assert.IsFalse(new LegacyAssetMovePolicy()
                .ShouldMove(legacyExists: false, destinationExists: false));
        }
    }
}
