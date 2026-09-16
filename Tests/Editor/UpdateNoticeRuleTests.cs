using FlowIoC.Editor.Help.WhatsNew;
using NUnit.Framework;
using UnityEditor.PackageManager;

namespace FlowIoC.Tests
{
    /// <summary>
    /// What the What's New tab says about the latest release. The rule has nothing of the Editor
    /// in it beyond the enum naming where a package came from, so every install road is read
    /// here rather than reproduced by installing the package five ways.
    /// </summary>
    public class UpdateNoticeRuleTests
    {
        private static UpdateNoticeEVO For(string installed, string latest, PackageSource source = PackageSource.Registry) =>
            new UpdateNoticeRule().For(installed, source, latest);

        [Test]
        public void Nothing_is_said_before_the_registry_has_answered()
        {
            Assert.AreEqual(UpdateNoticeKind.None, For("1.18.0", string.Empty).Kind);
        }

        /// <summary>
        /// A copy the Package Manager does not resolve has no version to compare, and a line
        /// about an update to nothing would be noise.
        /// </summary>
        [Test]
        public void Nothing_is_said_for_a_package_with_no_version()
        {
            Assert.AreEqual(UpdateNoticeKind.None, For(string.Empty, "1.18.0").Kind);
        }

        [Test]
        public void The_latest_release_installed_is_a_quiet_line()
        {
            UpdateNoticeEVO notice = For("1.18.0", "1.18.0");

            Assert.AreEqual(UpdateNoticeKind.UpToDate, notice.Kind);
            Assert.AreEqual("1.18.0 is the latest release.", notice.Text);
        }

        /// <summary>A maintainer's checkout runs ahead of what is released, and the line says so rather than lying.</summary>
        [Test]
        public void A_version_ahead_of_the_latest_release_says_so()
        {
            UpdateNoticeEVO notice = For("1.19.0", "1.18.0");

            Assert.AreEqual(UpdateNoticeKind.UpToDate, notice.Kind);
            Assert.AreEqual("1.19.0 is ahead of the latest release, 1.18.0.", notice.Text);
        }

        [Test]
        public void A_newer_release_is_a_note_that_names_both_versions()
        {
            UpdateNoticeEVO notice = For("1.17.1", "1.18.0");

            Assert.AreEqual(UpdateNoticeKind.Behind, notice.Kind);
            StringAssert.StartsWith("FlowIoC 1.18.0 is out; this project is on 1.17.1.", notice.Text);
        }

        [Test]
        public void A_registry_install_is_sent_to_the_Package_Manager()
        {
            StringAssert.Contains("Window > Package Manager", For("1.17.1", "1.18.0", PackageSource.Registry).Text);
        }

        [Test]
        public void A_Git_install_is_told_to_change_the_tag_in_the_manifest()
        {
            string text = For("1.17.1", "1.18.0", PackageSource.Git).Text;

            StringAssert.Contains("does not update a Git install", text);
            StringAssert.Contains("Packages/manifest.json", text);
        }

        /// <summary>
        /// The road that surprises people: a folder under Packages/ wins over the manifest, and
        /// the Package Manager shows no update because there is no list to update from.
        /// </summary>
        [Test]
        public void An_embedded_copy_is_told_the_Package_Manager_will_not_update_it()
        {
            string text = For("1.17.1", "1.18.0", PackageSource.Embedded).Text;

            StringAssert.Contains("does not update an embedded copy", text);
            StringAssert.Contains("install from the registry", text);
        }

        [Test]
        public void A_local_install_is_told_to_point_the_manifest_at_the_newer_copy()
        {
            StringAssert.Contains("does not update a local install", For("1.17.1", "1.18.0", PackageSource.Local).Text);
            StringAssert.Contains("does not update a local install", For("1.17.1", "1.18.0", PackageSource.LocalTarball).Text);
        }

        [Test]
        public void An_unknown_road_gets_the_fact_and_no_advice()
        {
            Assert.AreEqual("FlowIoC 1.18.0 is out; this project is on 1.17.1.",
                For("1.17.1", "1.18.0", PackageSource.Unknown).Text);
        }
    }
}
