#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FlowIoC.Editor.Console
{
    /// <summary>Where in a row's text a search term sits.</summary>
    public struct HighlightRange
    {
        public int Start;
        public int Length;
    }

    /// <summary>
    /// Which parts of a row matched the search. A narrowed list says which rows survived but not
    /// why, and on a long message the word that matched can be anywhere - so the console marks it,
    /// the way Unity's own console does.
    /// </summary>
    public class FlowConsoleHighlight
    {
        private readonly List<HighlightRange> _ranges = new();

        public List<HighlightRange> Ranges(string text, FlowConsoleSearchQuery query)
        {
            _ranges.Clear();

            if (string.IsNullOrEmpty(text)) return _ranges;
            if (query == null || query.IsBrokenPattern) return _ranges;

            for (int i = 0; i < query.Include.Count; i++)
                AddTerm(text, query.Include[i]);

            AddPattern(text, query.Pattern);

            _ranges.Sort((a, b) => a.Start.CompareTo(b.Start));

            Merge();

            return _ranges;
        }

        private void AddTerm(string text, string term)
        {
            if (string.IsNullOrEmpty(term)) return;

            int from = 0;

            while (from <= text.Length - term.Length)
            {
                int at = text.IndexOf(term, from, StringComparison.OrdinalIgnoreCase);
                if (at < 0) return;

                _ranges.Add(new HighlightRange {Start = at, Length = term.Length});
                from = at + 1;
            }
        }

        private void AddPattern(string text, Regex pattern)
        {
            if (pattern == null) return;

            foreach (Match match in pattern.Matches(text))
            {
                if (match.Length <= 0) continue;
                _ranges.Add(new HighlightRange {Start = match.Index, Length = match.Length});
            }
        }

        /// <summary>
        /// Two terms that overlap would paint one rectangle on top of another, and a translucent
        /// highlight painted twice is twice as dark.
        /// </summary>
        private void Merge()
        {
            for (int i = _ranges.Count - 2; i >= 0; i--)
            {
                HighlightRange current = _ranges[i];
                HighlightRange next = _ranges[i + 1];

                if (next.Start > current.Start + current.Length) continue;

                int end = Math.Max(current.Start + current.Length, next.Start + next.Length);

                _ranges[i] = new HighlightRange {Start = current.Start, Length = end - current.Start};
                _ranges.RemoveAt(i + 1);
            }
        }
    }
}
#endif
