#if UNITY_EDITOR

using System;
using System.Text.RegularExpressions;

namespace FlowIoC.Editor.ModuleCards
{
    /// <summary>
    /// The line a shipped module's card carries once its shape has settled, above the generated
    /// block beside the Version line:
    ///
    /// <code>
    /// Stage: stable
    /// </code>
    ///
    /// A versioned module without it is in beta: its data assets may change shape between
    /// versions, and the Module Library says so - BETA on its sidebar row, -beta after its version,
    /// a line in its update dialog. A stable module keeps the shape of what it ships: a field it
    /// renames carries [FormerlySerializedAs], so a game's own settings survive the update. A
    /// module installed once carries no version and no stage; it is the game's from the start.
    /// </summary>
    internal class ModuleCardStageLine
    {
        private const string BLOCK_BEGIN = "<!-- FLOWIOC:BEGIN";
        private const string STABLE = "stable";

        private static readonly Regex _line = new Regex(
            @"^\s*Stage:\s*(?<value>\S.*?)\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>Whether the card, above its block, says Stage: stable.</summary>
        internal bool IsStable(string cardText)
        {
            foreach (string line in (cardText ?? string.Empty).Replace("\r\n", "\n").Split('\n'))
            {
                if (line.TrimStart().StartsWith(BLOCK_BEGIN, StringComparison.Ordinal))
                    return false;

                Match match = _line.Match(line);

                if (match.Success)
                    return string.Equals(match.Groups["value"].Value, STABLE, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        /// <summary>
        /// Whether a shipped card marks its module as beta: versioned, and not stable. A card with
        /// no version is a module installed once, which has no stage.
        /// </summary>
        internal bool IsBeta(string cardText) =>
            new ModuleCardVersionLine().Has(cardText) && !IsStable(cardText);
    }
}

#endif
