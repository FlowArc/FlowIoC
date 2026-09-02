using FlowIoC.Editor.Help.WhatsNew;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// Whether opening the Editor is an occasion to show what changed. The rule has nothing of
    /// Unity in it, so the awkward cases can be read here rather than reproduced by installing
    /// versions of the package.
    /// </summary>
    public class WhatsNewNoticeRuleTests
    {
        private static WhatsNewDecision For(string installed, string seen) =>
            new WhatsNewNoticeRule().For(installed, seen);

        [Test]
        public void A_version_the_reader_has_already_seen_is_not_shown_again()
        {
            Assert.AreEqual(WhatsNewDecision.Stop, For("1.5.0", "1.5.0"));
        }

        [Test]
        public void A_version_the_reader_has_not_seen_is_shown()
        {
            Assert.AreEqual(WhatsNewDecision.Show, For("1.5.0", "1.4.0"));
        }

        /// <summary>
        /// Nothing recorded means this reader has never opened this project with FlowIoC in it.
        /// What they want then is the introduction, not a list of what changed in a package they
        /// have not used yet - so the version is recorded quietly and the next update is the first
        /// one they are shown.
        /// </summary>
        [Test]
        public void A_reader_who_has_seen_nothing_is_recorded_rather_than_shown()
        {
            Assert.AreEqual(WhatsNewDecision.RecordOnly, For("1.5.0", string.Empty));
        }

        /// <summary>
        /// An unresolved package has no version to compare or to record, which is what an
        /// embedded copy outside the Package Manager looks like.
        /// </summary>
        [Test]
        public void An_unknown_installed_version_settles_nothing()
        {
            Assert.AreEqual(WhatsNewDecision.Stop, For(string.Empty, "1.4.0"));
        }

        /// <summary>
        /// Downgrading is still a change of version, and the reader is better served by the notes
        /// of what they are now on than by silence.
        /// </summary>
        [Test]
        public void Going_back_to_an_older_version_is_shown_too()
        {
            Assert.AreEqual(WhatsNewDecision.Show, For("1.4.0", "1.5.0"));
        }
    }
}
