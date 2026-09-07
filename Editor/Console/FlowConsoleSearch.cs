#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FlowIoC.Editor.Console
{
    /// <summary>
    /// One parsed search box. Terms are ANDed, a term starting with '-' excludes, and a term
    /// wrapped in slashes is a regular expression.
    /// </summary>
    public class FlowConsoleSearchQuery
    {
        internal readonly List<string> Include = new();
        internal readonly List<string> Exclude = new();
        internal Regex Pattern;
        internal bool IsBrokenPattern;

        public bool Matches(string message)
        {
            if (IsBrokenPattern) return false;

            bool empty = Include.Count == 0 && Exclude.Count == 0 && Pattern == null;
            if (empty) return true;

            if (message == null) return false;

            for (int i = 0; i < Exclude.Count; i++)
            {
                if (message.IndexOf(Exclude[i], StringComparison.OrdinalIgnoreCase) >= 0)
                    return false;
            }

            for (int i = 0; i < Include.Count; i++)
            {
                if (message.IndexOf(Include[i], StringComparison.OrdinalIgnoreCase) < 0)
                    return false;
            }

            return Pattern == null || Pattern.IsMatch(message);
        }
    }

    /// <summary>
    /// Reads what was typed into the search box. A plain term narrows, a '-' term excludes - which
    /// is what silences one noisy loop without hiding the channel it shares with everything else -
    /// and slashes make the whole thing a regular expression.
    /// </summary>
    public class FlowConsoleSearch
    {
        public FlowConsoleSearchQuery Parse(string text)
        {
            var query = new FlowConsoleSearchQuery();
            if (string.IsNullOrWhiteSpace(text)) return query;

            string trimmed = text.Trim();

            if (trimmed.Length > 1 && trimmed[0] == '/' && trimmed[trimmed.Length - 1] == '/')
            {
                string pattern = trimmed.Substring(1, trimmed.Length - 2);

                try
                {
                    query.Pattern = new Regex(pattern, RegexOptions.IgnoreCase);
                }
                catch (ArgumentException)
                {
                    // Half an expression is what a search box holds most of the time. It matches
                    // nothing rather than throwing on every repaint.
                    query.IsBrokenPattern = true;
                }

                return query;
            }

            foreach (string part in trimmed.Split(' '))
            {
                if (part.Length == 0) continue;

                if (part[0] == '-' && part.Length > 1)
                    query.Exclude.Add(part.Substring(1));
                else
                    query.Include.Add(part);
            }

            return query;
        }
    }
}
#endif
