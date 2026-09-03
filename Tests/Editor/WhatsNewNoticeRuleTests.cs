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
        private static WhatsNewDecision For(string installed, string seen, string setup = "") =>
            new WhatsNewNoticeRule().For(installed, seen, setup);

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
        /// Nothing recorded and no marker means this reader has never opened this project with
        /// FlowIoC in it. What they want then is the introduction, not a list of what changed in a
        /// package they have not used yet - and the introduction is what they get, because nothing
        /// else in the Editor tells a new project where to start.
        /// </summary>
        [Test]
        public void A_reader_who_has_seen_nothing_in_a_project_with_no_marker_is_introduced()
        {
            Assert.AreEqual(WhatsNewDecision.Introduce, For("1.5.0", string.Empty));
        }

        /// <summary>
        /// A marker naming the version now installed is a project that met FlowIoC at this version,
        /// so the reader is still meeting it and still wants the introduction.
        /// </summary>
        [Test]
        public void A_project_that_met_FlowIoC_at_the_installed_version_is_introduced()
        {
            Assert.AreEqual(WhatsNewDecision.Introduce, For("1.5.0", string.Empty, "1.5.0"));
        }

        /// <summary>
        /// The case the feature could not otherwise serve: the release that introduces What's New
        /// lands in a project that has been on FlowIoC for a while, so every reader has nothing
        /// recorded on that day. The marker names the older version the setup modules were
        /// installed at, which is the project saying it has been here before.
        /// </summary>
        [Test]
        public void A_project_that_has_been_here_before_is_shown_even_with_nothing_recorded()
        {
            Assert.AreEqual(WhatsNewDecision.Show, For("1.5.0", string.Empty, "1.4.0"));
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