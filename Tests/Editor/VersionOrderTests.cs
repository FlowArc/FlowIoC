using FlowIoC.Editor.Help.WhatsNew;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// Which of two versions the update notice takes for the newer one. The cases that matter are
    /// the ones a string comparison gets wrong - a minor of 18 against a minor of 9, a pre-release
    /// against the release it leads to - and the strings that are not versions at all.
    /// </summary>
    public class VersionOrderTests
    {
        private static bool IsNewer(string candidate, string current) =>
            new VersionOrder().IsNewer(candidate, current);

        [Test]
        public void A_later_patch_is_newer()
        {
            Assert.IsTrue(IsNewer("1.18.1", "1.18.0"));
        }

        [Test]
        public void A_later_minor_is_newer_even_when_it_sorts_earlier_as_text()
        {
            Assert.IsTrue(IsNewer("1.18.0", "1.9.0"));
            Assert.IsFalse(IsNewer("1.9.0", "1.18.0"));
        }

        [Test]
        public void A_later_major_outranks_any_minor()
        {
            Assert.IsTrue(IsNewer("2.0.0", "1.99.9"));
        }

        [Test]
        public void The_same_version_is_not_newer()
        {
            Assert.IsFalse(IsNewer("1.18.0", "1.18.0"));
        }

        [Test]
        public void A_release_is_newer_than_its_own_pre_release()
        {
            Assert.IsTrue(IsNewer("1.18.0", "1.18.0-preview.1"));
            Assert.IsFalse(IsNewer("1.18.0-preview.1", "1.18.0"));
        }

        [Test]
        public void Two_pre_releases_are_ordered_by_their_tags()
        {
            Assert.IsTrue(IsNewer("1.18.0-preview.2", "1.18.0-preview.1"));
        }

        [Test]
        public void A_missing_patch_reads_as_zero()
        {
            Assert.IsFalse(IsNewer("1.18", "1.18.0"));
            Assert.IsTrue(IsNewer("1.19", "1.18.0"));
        }

        [Test]
        public void A_string_that_is_not_a_version_is_never_newer()
        {
            Assert.IsFalse(IsNewer("latest", "1.18.0"));
            Assert.IsFalse(IsNewer(string.Empty, "1.18.0"));
            Assert.IsFalse(IsNewer(null, "1.18.0"));
        }

        [Test]
        public void Any_version_is_newer_than_no_version()
        {
            Assert.IsTrue(IsNewer("1.18.0", string.Empty));
            Assert.IsTrue(IsNewer("1.18.0", "not a version"));
        }
    }
}
