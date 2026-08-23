using FlowIoC.Editor.Migration;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class LegacyFolderCleanupPolicyTests
    {
        [Test]
        public void An_emptied_legacy_folder_is_deleted()
        {
            Assert.IsTrue(new LegacyFolderCleanupPolicy().ShouldDelete(exists: true, isEmpty: true));
        }

        /// <summary>
        /// Assets/Editor and Assets/Resources are shared with the game. FlowIoC only ever removes
        /// them when its own files were the last thing in them.
        /// </summary>
        [Test]
        public void A_folder_that_still_holds_project_files_is_left_alone()
        {
            Assert.IsFalse(new LegacyFolderCleanupPolicy().ShouldDelete(exists: true, isEmpty: false));
        }

        [Test]
        public void A_folder_that_is_already_gone_is_not_deleted_again()
        {
            Assert.IsFalse(new LegacyFolderCleanupPolicy().ShouldDelete(exists: false, isEmpty: true));
        }
    }
}
