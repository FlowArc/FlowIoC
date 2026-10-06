#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FlowIoC.Editor.ModuleCards
{
    /// <summary>
    /// The files a shipped module hands the game to extend, named on its card above the generated
    /// block, one line each and relative to the module:
    ///
    /// <code>
    /// Extend: Scripts/Shared/Enums/CameraName.cs
    /// </code>
    ///
    /// An update reads such a file as the game's work even though it is code: where both sides
    /// changed it, the reader is asked, the way a data asset is, instead of the package's version
    /// replacing the game's. Camera's camera names are the case it exists for - the module ships
    /// two and every game adds its own.
    /// </summary>
    internal class ModuleCardExtendLine
    {
        private const string BLOCK_BEGIN = "<!-- FLOWIOC:BEGIN";

        private static readonly Regex _line = new Regex(
            @"^\s*Extend:\s*(?<value>\S.*?)\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>Every path the card's Extend lines name, forward-slashed, in the order written.</summary>
        internal IReadOnlyList<string> Read(string cardText)
        {
            var paths = new List<string>();

            foreach (string line in (cardText ?? string.Empty).Replace("\r\n", "\n").Split('\n'))
            {
                if (line.TrimStart().StartsWith(BLOCK_BEGIN, StringComparison.Ordinal))
                    break;

                Match match = _line.Match(line);

                if (match.Success)
                    paths.Add(match.Groups["value"].Value.Replace('\\', '/').TrimStart('/'));
            }

            return paths;
        }
    }
}

#endif
