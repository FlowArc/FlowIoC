#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;

namespace FlowIoC.Editor.Help.WhatsNew
{
    /// <summary>
    /// Reads CHANGELOG.md into the headlines the What's New tab shows.
    ///
    /// The changelog is written for somebody working out what changed and why, so an entry runs
    /// to a paragraph and often carries the detail underneath it as indented bullets. A reader
    /// who has just updated the package wants neither: they want the line that says what landed.
    /// So every entry is reduced to its first sentence and everything indented under it is
    /// dropped. Reading the file rather than keeping a second one beside it is what leaves one
    /// thing to write per release.
    /// </summary>
    internal class WhatsNewReading
    {
        private const string VERSION_HEADING = "## ";
        private const string GROUP_HEADING = "### ";
        private const string ENTRY = "- ";

        internal IReadOnlyList<WhatsNewVersionEVO> Of(string changelog)
        {
            var releases = new List<WhatsNewVersionEVO>();

            if (string.IsNullOrWhiteSpace(changelog))
                return releases;

            List<WhatsNewGroupEVO> groups = null;
            List<string> lines = null;
            var entry = new StringBuilder();

            foreach (string raw in changelog.Split('\n'))
            {
                string line = raw.TrimEnd('\r');

                if (line.StartsWith(VERSION_HEADING))
                {
                    Close(entry, lines);

                    groups = new List<WhatsNewGroupEVO>();
                    lines = null;
                    releases.Add(ReleaseFrom(line, groups));

                    continue;
                }

                if (line.StartsWith(GROUP_HEADING))
                {
                    Close(entry, lines);

                    // A section above the first version heading belongs to the file, not to a
                    // release, so there is nothing to hang it on.
                    if (groups == null) continue;

                    lines = new List<string>();
                    groups.Add(new WhatsNewGroupEVO {Title = line.Substring(GROUP_HEADING.Length).Trim(), Lines = lines});

                    continue;
                }

                if (line.StartsWith(ENTRY))
                {
                    Close(entry, lines);
                    entry.Append(line.Substring(ENTRY.Length).Trim());

                    continue;
                }

                if (line.Trim().Length == 0)
                {
                    Close(entry, lines);

                    continue;
                }

                Continue(line, entry, lines);
            }

            Close(entry, lines);

            return releases;
        }

        /// <summary>
        /// A line under the entry that is still being read. An indented bullet is the detail the
        /// tab exists to leave out, so it closes the entry above it and adds nothing of its own -
        /// which also means its own wrapped lines find no open entry to join.
        /// </summary>
        private void Continue(string line, StringBuilder entry, List<string> lines)
        {
            string text = line.Trim();

            if (text.StartsWith(ENTRY))
            {
                Close(entry, lines);

                return;
            }

            if (entry.Length == 0) return;

            entry.Append(' ').Append(text);
        }

        private void Close(StringBuilder entry, List<string> lines)
        {
            if (entry.Length == 0) return;

            string headline = Headline(entry.ToString());

            entry.Clear();

            if (lines != null && headline.Length > 0)
                lines.Add(headline);
        }

        /// <summary>
        /// `## [1.4.0] - 2026-09-02`, and `## [Unreleased]` for the section that has no date yet.
        /// A heading written without the brackets is taken whole rather than refused, because a
        /// changelog that drifts from Keep a Changelog should still be readable.
        /// </summary>
        private WhatsNewVersionEVO ReleaseFrom(string line, List<WhatsNewGroupEVO> groups)
        {
            string text = line.Substring(VERSION_HEADING.Length).Trim();
            string version = text;
            string date = string.Empty;

            int open = text.IndexOf('[');
            int close = text.IndexOf(']');

            if (open >= 0 && close > open)
            {
                version = text.Substring(open + 1, close - open - 1).Trim();
                date = text.Substring(close + 1).TrimStart(' ', '-').Trim();
            }

            return new WhatsNewVersionEVO {Version = version, Date = date, Groups = groups};
        }

        /// <summary>
        /// The first sentence, with the markdown that made the entry readable on GitHub taken
        /// out - the painter draws plain text, so an emphasis marker would be read as
        /// punctuation, and one sitting between two sentences would hide the end of the first.
        ///
        /// An asterisk inside a code span is a wildcard rather than markdown, so which characters
        /// came from inside a span is carried alongside the text: the same answer tells the
        /// sentence where a dot is part of a file name.
        /// </summary>
        private string Headline(string entry)
        {
            var text = new StringBuilder();
            var inCodeSpan = new List<bool>();
            var inCode = false;

            foreach (char character in entry)
            {
                if (character == '`')
                {
                    inCode = !inCode;

                    continue;
                }

                if (character == '*' && !inCode) continue;

                text.Append(character);
                inCodeSpan.Add(inCode);
            }

            return FirstSentence(text.ToString(), inCodeSpan).Trim();
        }

        /// <summary>
        /// Where the first sentence ends. A dot inside a code span is part of a file name, and a
        /// dot inside a version number has no space after it, so a sentence ends only where the
        /// dot is followed by whitespace and something that reads as a new sentence.
        /// </summary>
        private string FirstSentence(string text, IReadOnlyList<bool> inCodeSpan)
        {
            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] != '.' || inCodeSpan[i]) continue;

                if (i == text.Length - 1) return text;

                if (!char.IsWhiteSpace(text[i + 1])) continue;

                int next = i + 1;

                while (next < text.Length && char.IsWhiteSpace(text[next]))
                    next++;

                if (next >= text.Length) return text.Substring(0, i + 1);

                if (char.IsUpper(text[next]) || inCodeSpan[next])
                    return text.Substring(0, i + 1);
            }

            return text;
        }
    }
}

#endif