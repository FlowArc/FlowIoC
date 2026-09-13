#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FlowIoC.Editor.ModuleCards
{
    /// <summary>What a card says about its module's channel, as written: the hex after Colour and the text after Profile.</summary>
    internal class ModuleCardChannelLinesEVO
    {
        internal string Colour { get; set; }
        internal string Profile { get; set; }
    }

    /// <summary>
    /// The two lines a module's card may carry about its Flow Console channel, directly above the
    /// generated block:
    ///
    /// <code>
    /// Colour: #E5A50A
    /// Profile: prefix="[Main]" prefix-style=bold
    /// </code>
    ///
    /// They are the one part of the authored half a tool writes, because the panel that edits a
    /// channel's colour edits the card rather than a shared asset: the module owns its colour the
    /// way it owns its channel, and the card is where a module says what is true of it. Read from
    /// anywhere above the block, so a line somebody moved under a heading still counts; written
    /// above the block, where the next reader will look for them.
    /// </summary>
    internal class ModuleCardChannelLines
    {
        private const string BLOCK_BEGIN = "<!-- FLOWIOC:BEGIN";
        private const string COLOUR_KEY = "Colour: ";
        private const string PROFILE_KEY = "Profile: ";

        private static readonly Regex ColourLine = new Regex(
            @"^\s*Colou?r:\s*(?<value>\S.*?)\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex ProfileLine = new Regex(
            @"^\s*Profile:\s*(?<value>\S.*?)\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        internal ModuleCardChannelLinesEVO Read(string cardText)
        {
            var lines = new ModuleCardChannelLinesEVO();

            foreach (string line in AuthoredLines(cardText))
            {
                Match colour = ColourLine.Match(line);
                if (colour.Success && lines.Colour == null)
                {
                    lines.Colour = colour.Groups["value"].Value;
                    continue;
                }

                Match profile = ProfileLine.Match(line);
                if (profile.Success && lines.Profile == null)
                    lines.Profile = profile.Groups["value"].Value;
            }

            return lines;
        }

        /// <summary>
        /// The card with these lines and no others of their kind. Null drops a line, so a module
        /// put back on the palette loses its Colour line rather than keeping a stale one. The
        /// generated block and everything the author wrote stay as they were.
        /// </summary>
        internal string Write(string cardText, string colour, string profile)
        {
            string text = cardText ?? string.Empty;
            string newLine = text.IndexOf("\r\n", StringComparison.Ordinal) >= 0 ? "\r\n" : "\n";
            string[] all = text.Replace("\r\n", "\n").Split('\n');

            int blockAt = IndexOfBlock(all);
            var authored = new List<string>();
            var rest = new List<string>();

            for (int index = 0; index < all.Length; index++)
            {
                bool inAuthored = blockAt < 0 || index < blockAt;

                if (inAuthored && (ColourLine.IsMatch(all[index]) || ProfileLine.IsMatch(all[index])))
                    continue;

                (inAuthored ? authored : rest).Add(all[index]);
            }

            while (authored.Count > 0 && authored[authored.Count - 1].Trim().Length == 0)
                authored.RemoveAt(authored.Count - 1);

            var written = new List<string>();
            if (!string.IsNullOrEmpty(colour)) written.Add(COLOUR_KEY + colour.Trim());
            if (!string.IsNullOrEmpty(profile)) written.Add(PROFILE_KEY + profile.Trim());

            if (written.Count > 0)
            {
                if (authored.Count > 0) authored.Add(string.Empty);
                authored.AddRange(written);
            }

            if (rest.Count > 0)
            {
                authored.Add(string.Empty);
                authored.AddRange(rest);
            }
            else
            {
                authored.Add(string.Empty);
            }

            return string.Join(newLine, authored);
        }

        private static IEnumerable<string> AuthoredLines(string cardText)
        {
            string[] lines = (cardText ?? string.Empty).Replace("\r\n", "\n").Split('\n');
            int blockAt = IndexOfBlock(lines);

            for (int index = 0; index < lines.Length; index++)
            {
                if (blockAt >= 0 && index >= blockAt) yield break;

                yield return lines[index];
            }
        }

        private static int IndexOfBlock(string[] lines)
        {
            for (int index = 0; index < lines.Length; index++)
            {
                if (lines[index].TrimStart().StartsWith(BLOCK_BEGIN, StringComparison.Ordinal))
                    return index;
            }

            return -1;
        }
    }
}

#endif
