#if UNITY_EDITOR

using System;

namespace FlowIoC.Editor.ModuleCards
{
    /// <summary>
    /// Card text in, the two lines the directory quotes out. It never touches the filesystem,
    /// which is what makes the awkward cases - a heading with nothing under it, a card that is
    /// still the stub, a file saved with CRLF - cheap to test.
    /// </summary>
    internal class ModuleCardReader
    {
        private const string PURPOSE_HEADING = "## Purpose";
        private const string CONCEPTS_HEADING = "## Concepts";
        private const string BLOCK_BEGIN = "<!-- FLOWIOC:BEGIN";

        internal ModuleCardAuthoredEVO Read(string cardText)
        {
            string purpose = FirstLineUnder(cardText, PURPOSE_HEADING);
            string concepts = FirstLineUnder(cardText, CONCEPTS_HEADING);

            return new ModuleCardAuthoredEVO
            {
                Purpose = purpose,
                Concepts = concepts,
                PurposeIsPlaceholder = IsPlaceholder(purpose),
                ConceptsIsPlaceholder = IsPlaceholder(concepts),
            };
        }

        /// <summary>
        /// A line wrapped in single underscores is the stub's italics, and an absent line is the
        /// same thing said by omission. Both mean the author has not answered yet.
        /// </summary>
        private bool IsPlaceholder(string line)
        {
            return string.IsNullOrEmpty(line)
                   || (line.StartsWith("_", StringComparison.Ordinal)
                       && line.EndsWith("_", StringComparison.Ordinal));
        }

        private string FirstLineUnder(string cardText, string heading)
        {
            string text = (cardText ?? string.Empty).Replace("\r\n", "\n").Replace("\r", "\n");
            string[] lines = text.Split('\n');

            int start = -1;

            for (int index = 0; index < lines.Length; index++)
            {
                if (lines[index].Trim() == heading)
                {
                    start = index + 1;
                    break;
                }
            }

            if (start < 0) return null;

            for (int index = start; index < lines.Length; index++)
            {
                string line = lines[index].Trim();

                if (line.Length == 0) continue;

                // Anything that opens a new region ends this one, and both are the honest report
                // that the heading was left empty.
                if (line.StartsWith("#", StringComparison.Ordinal)) return null;
                if (line.StartsWith(BLOCK_BEGIN, StringComparison.Ordinal)) return null;

                return line;
            }

            return null;
        }
    }
}

#endif
