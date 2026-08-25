using FlowIoC.Editor.CodeGenerator;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class FolderRenamePlannerTests
    {
        private const string ControllersPath =
            @"D:\Project\Assets\Modules\CameraModule\Scripts\Runtime\Controllers";

        [Test]
        public void A_folder_already_named_correctly_needs_no_rename()
        {
            bool result = new FolderRenamePlanner().TryPlanRename(ControllersPath, "Controllers", out string newPath);

            Assert.IsFalse(result);
            Assert.IsNull(newPath);
        }

        /// <summary>
        /// A folder that differs only by case is not treated as misnamed - renaming it to itself
        /// would be a no-op at best and a needless AssetDatabase churn at worst.
        /// </summary>
        [Test]
        public void A_case_only_difference_needs_no_rename()
        {
            bool result = new FolderRenamePlanner().TryPlanRename(
                @"D:\Project\Assets\Modules\CameraModule\Scripts\Runtime\controllers", "Controllers", out _);

            Assert.IsFalse(result);
        }

        [Test]
        public void A_differently_named_folder_is_renamed_in_place()
        {
            bool result = new FolderRenamePlanner().TryPlanRename(ControllersPath, "Commands", out string newPath);

            Assert.IsTrue(result);
            Assert.AreEqual(@"D:\Project\Assets\Modules\CameraModule\Scripts\Runtime\Commands", newPath);
        }

        [Test]
        public void A_trailing_separator_does_not_change_the_decision()
        {
            bool result = new FolderRenamePlanner().TryPlanRename(ControllersPath + @"\", "Commands", out string newPath);

            Assert.IsTrue(result);
            Assert.AreEqual(@"D:\Project\Assets\Modules\CameraModule\Scripts\Runtime\Commands", newPath);
        }

        [Test]
        public void An_empty_current_path_needs_no_rename()
        {
            Assert.IsFalse(new FolderRenamePlanner().TryPlanRename(string.Empty, "Commands", out _));
        }

        [Test]
        public void An_empty_configured_name_needs_no_rename()
        {
            Assert.IsFalse(new FolderRenamePlanner().TryPlanRename(ControllersPath, string.Empty, out _));
        }
    }
}
