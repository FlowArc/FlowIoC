using FlowIoC.ConsoleModule;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class FlowConsoleSettingsCreationPolicyTests
    {
        [Test]
        public void Nothing_is_created_when_the_asset_loaded()
        {
            Assert.IsFalse(new FlowConsoleSettingsCreationPolicy()
                .ShouldCreate(assetLoaded: true, fileExistsOnDisk: true));
        }

        [Test]
        public void The_asset_is_created_when_it_is_absent_from_disk()
        {
            Assert.IsTrue(new FlowConsoleSettingsCreationPolicy()
                .ShouldCreate(assetLoaded: false, fileExistsOnDisk: false));
        }

        /// <summary>
        /// The failure this whole type exists for: renaming the package breaks Unity's
        /// script-to-type association, so the settings asset stops loading even though the
        /// file is sitting right there. Creating a fresh one overwrites the user's log types.
        /// </summary>
        [Test]
        public void Nothing_is_created_when_the_file_exists_but_did_not_load()
        {
            Assert.IsFalse(new FlowConsoleSettingsCreationPolicy()
                .ShouldCreate(assetLoaded: false, fileExistsOnDisk: true));
        }

        [Test]
        public void A_loaded_asset_with_no_file_on_disk_still_creates_nothing()
        {
            Assert.IsFalse(new FlowConsoleSettingsCreationPolicy()
                .ShouldCreate(assetLoaded: true, fileExistsOnDisk: false));
        }
    }
}
