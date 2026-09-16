#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FlowIoC.Editor.ModuleCards
{
    /// <summary>
    /// The one line a module's card carries about its version, directly above the generated
    /// block beside the Colour and Profile lines:
    ///
    /// <code>
    /// Version: 1.2.0
    /// </code>
    ///
    /// A shipped module's version is this line and nothing else. The publisher raises it in the
    /// host's card, the installer copies it in with the card, and the updater writes the new one
    /// after an update. A card without the line reads as 0.0.0, which is what lets a module
    /// installed before the line existed be offered its first update.
    /// </summary>
    internal class ModuleCardVersionLine
    {
        internal const string NONE = "0.0.0";

        private const string BLOCK_BEGIN = "<!-- FLOWIOC:BEGIN";
        private const string KEY = "Version: ";

        private static readonly Regex Line = new Regex(
            @"^\s*Version:\s*(?<value>\S.*?)\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // The other lines a tool reads off the card. A version written directly under one of
        // them keeps the three together, which is where the next reader looks for them.
        private static readonly Regex ToolLine = new Regex(
            @"^\s*(Colou?r|Profile|Publish):", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        internal static bool IsLine(string line) => Line.IsMatch(line ?? string.Empty);

        /// <summary>The version above the block, or 0.0.0 when the card carries none.</summary>
        internal string Read(string cardText)
        {
            foreach (string line in AuthoredLines(cardText))
            {
                Match match = Line.Match(line);

                if (match.Success)
                    return match.Groups["value"].Value;
            }

            return NONE;
        }

        internal bool Has(string cardText)
        {
            foreach (string line in AuthoredLines(cardText))
            {
                if (Line.IsMatch(line))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// The card with this one version line and no other: any line of its kind above the block
        /// is dropped and the new one written directly above the block. What the author wrote and
        /// the block itself stay as they were.
        /// </summary>
        internal string Write(string cardText, string version)
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

                if (inAuthored && Line.IsMatch(all[index]))
                    continue;

                (inAuthored ? authored : rest).Add(all[index]);
            }

            while (authored.Count > 0 && authored[authored.Count - 1].Trim().Length == 0)
                authored.RemoveAt(authored.Count - 1);

            if (authored.Count > 0 && !ToolLine.IsMatch(authored[authored.Count - 1]))
                authored.Add(string.Empty);

            authored.Add(KEY + version.Trim());
            authored.Add(string.Empty);

            if (rest.Count > 0)
                authored.AddRange(rest);

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
