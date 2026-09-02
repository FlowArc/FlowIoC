using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlowIoC.Editor.Help.WhatsNew;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The tab against the changelog the package actually ships. WhatsNewReadingTests pin the
    /// reading rules on text a test writes; these pin the other half - that the file is there to
    /// read, and that what FlowIoC writes into it still comes out as headlines.
    ///
    /// An entry whose first sentence is a paragraph is a changelog entry that needs rewriting,
    /// not a parser that needs loosening.
    /// </summary>
    public class WhatsNewSourceTests
    {
        private const int LONGEST_HEADLINE = 400;

        private static IReadOnlyList<string> Headlines() =>
            new WhatsNewSource().Releases()
                .SelectMany(release => release.Groups)
                .SelectMany(group => group.Lines)
                .ToList();

        [Test]
        public void The_package_ships_the_changelog_the_tab_reads()
        {
            string path = new WhatsNewSource().ChangelogPath;

            Assert.IsTrue(File.Exists(path), $"The package no longer ships '{path}'.");
        }

        [Test]
        public void The_shipped_changelog_reads_as_releases()
        {
            IReadOnlyList<WhatsNewVersionEVO> releases = new WhatsNewSource().Releases();

            Assert.Greater(releases.Count, 0, "The changelog produced no releases at all.");
            Assert.Greater(Headlines().Count, 0, "The changelog produced no entries at all.");
        }

        /// <summary>
        /// The painter draws plain text, so a marker left in would be read as punctuation.
        /// </summary>
        [Test]
        public void No_headline_carries_markdown_through()
        {
            foreach (string headline in Headlines())
            {
                Assert.IsFalse(headline.Contains("`"), $"A code span survived: '{headline}'.");
                Assert.IsFalse(headline.Contains("*"), $"An emphasis marker survived: '{headline}'.");
                Assert.IsNotEmpty(headline);
            }
        }

        [Test]
        public void No_headline_runs_on_past_a_readable_line()
        {
            foreach (string headline in Headlines())
            {
                Assert.LessOrEqual(headline.Length, LONGEST_HEADLINE,
                    $"This entry's first sentence is too long to be a headline: '{headline}'.");
            }
        }
    }
}
