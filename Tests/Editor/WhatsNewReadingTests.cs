using System.Collections.Generic;
using System.Linq;
using FlowIoC.Editor.Help.WhatsNew;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// What the Help window's What's New tab reads out of CHANGELOG.md.
    ///
    /// The changelog is written for somebody working out what changed and why, so its entries run
    /// to paragraphs. The tab is read by somebody who has just updated the package and wants the
    /// headlines, so every entry is reduced to its first sentence and the detail below it is
    /// dropped. Reading rather than rewriting is what keeps one file to maintain per release.
    /// </summary>
    public class WhatsNewReadingTests
    {
        private const string CHANGELOG = @"# Changelog

All notable changes to this package are documented in this file.

## [Unreleased]

### Fixed

- **A generated module's signal holders no longer keep `Scripts` in their namespace.** The
  generator wrote its skip list into each assembly and read it back from a file that never
  existed.

## [1.4.0] - 2026-09-02

### Added

- **One visual language across every inspector.** A component wears a bar that says what it is.
  - A Root takes the colour of whatever it roots.
  - The sub-context list badges connector entries.
- Module assembly names are built in one tested place. Four copies read the suffix off the name.

### Changed

- Easy Save 3.5.27 has no async API at all, so `ES3.cs` answers nothing.

### Removed

- The Screen Config Manager window.
";

        private static IReadOnlyList<WhatsNewVersionEVO> Read() => new WhatsNewReading().Of(CHANGELOG);

        private static WhatsNewVersionEVO Version(string version) =>
            Read().Single(release => release.Version == version);

        private static IReadOnlyList<string> Lines(string version, string group) =>
            Version(version).Groups.Single(entry => entry.Title == group).Lines;

        [Test]
        public void A_version_heading_becomes_a_release_with_its_date()
        {
            Assert.AreEqual("2026-09-02", Version("1.4.0").Date);
        }

        [Test]
        public void An_unreleased_heading_becomes_a_release_with_no_date()
        {
            Assert.AreEqual(string.Empty, Version("Unreleased").Date);
        }

        /// <summary>
        /// Newest first, the way the file is written. The reader wants what just landed, not the
        /// history of the package.
        /// </summary>
        [Test]
        public void The_releases_come_back_in_the_order_the_file_lists_them()
        {
            CollectionAssert.AreEqual(
                new[] {"Unreleased", "1.4.0"},
                Read().Select(release => release.Version).ToList());
        }

        [Test]
        public void A_release_keeps_the_sections_it_was_written_with()
        {
            CollectionAssert.AreEqual(
                new[] {"Added", "Changed", "Removed"},
                Version("1.4.0").Groups.Select(group => group.Title).ToList());
        }

        /// <summary>
        /// The paragraph under the headline is what the changelog is for and what the tab is not.
        /// </summary>
        [Test]
        public void An_entry_is_reduced_to_its_first_sentence()
        {
            Assert.AreEqual(
                "One visual language across every inspector.",
                Lines("1.4.0", "Added")[0]);
        }

        /// <summary>
        /// An entry wrapped over two lines is one entry, so the sentence has to be found in the
        /// text as it reads rather than in the first line of it.
        /// </summary>
        [Test]
        public void An_entry_wrapped_over_two_lines_is_still_one_entry()
        {
            Assert.AreEqual(
                "Module assembly names are built in one tested place.",
                Lines("1.4.0", "Added")[1]);
        }

        [Test]
        public void An_indented_entry_is_detail_and_is_dropped()
        {
            Assert.AreEqual(2, Lines("1.4.0", "Added").Count);
        }

        /// <summary>
        /// A version number and a file name both carry a dot that ends nothing, so the sentence
        /// ends at a dot only where the next thing is a new sentence.
        /// </summary>
        [Test]
        public void A_dot_inside_a_version_number_or_a_file_name_does_not_end_the_sentence()
        {
            Assert.AreEqual(
                "Easy Save 3.5.27 has no async API at all, so ES3.cs answers nothing.",
                Lines("1.4.0", "Changed")[0]);
        }

        /// <summary>
        /// The painter draws plain text, so the markdown that made the changelog readable on
        /// GitHub would be read out as punctuation here.
        /// </summary>
        [Test]
        public void Bold_and_code_markers_are_stripped()
        {
            Assert.AreEqual(
                "A generated module's signal holders no longer keep Scripts in their namespace.",
                Lines("Unreleased", "Fixed")[0]);
        }

        /// <summary>
        /// The changelog italicises a menu path, and an entry whose headline is followed by one
        /// would run into the sentence after it if the marker were left in - the dot would be
        /// followed by an asterisk rather than by the capital that starts the next sentence.
        /// </summary>
        [Test]
        public void An_italic_marker_does_not_hide_the_end_of_the_sentence()
        {
            IReadOnlyList<WhatsNewVersionEVO> releases = new WhatsNewReading().Of(
                "## [1.5.0] - 2026-09-03\n\n### Fixed\n\n"
                + "- **The holders no longer keep `Scripts`.** *Create Module* wrote the long one.\n");

            Assert.AreEqual("The holders no longer keep Scripts.", releases[0].Groups[0].Lines[0]);
        }

        /// <summary>
        /// An asterisk inside a code span is a wildcard rather than markdown, so it is part of
        /// what the entry says.
        /// </summary>
        [Test]
        public void An_asterisk_inside_a_code_span_is_kept()
        {
            IReadOnlyList<WhatsNewVersionEVO> releases = new WhatsNewReading().Of(
                "## [1.5.0] - 2026-09-03\n\n### Fixed\n\n"
                + "- The pattern `*.asmdef` is matched. Nothing else is.\n");

            Assert.AreEqual("The pattern *.asmdef is matched.", releases[0].Groups[0].Lines[0]);
        }

        [Test]
        public void The_file_header_above_the_first_version_is_not_a_release()
        {
            Assert.AreEqual(2, Read().Count);
        }

        [Test]
        public void A_changelog_with_nothing_in_it_reads_as_no_releases()
        {
            CollectionAssert.IsEmpty(new WhatsNewReading().Of(string.Empty));
        }
    }
}