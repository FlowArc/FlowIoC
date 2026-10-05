#if UNITY_EDITOR

using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace FlowIoC.Editor.Inspector
{
    /// <summary>
    /// Turns a C# file into the help text its members carry. The documentation a field already
    /// has is the documentation the inspector shows, so nothing is written twice and a module
    /// that comments its own fields gets the help for free.
    ///
    /// It reads text, not syntax: a doc comment belongs to the first declaration under it, and a
    /// blank line between the two means the comment belongs to neither.
    /// </summary>
    internal class FlowHelpParser
    {
        /// <summary>Where the summary of the type itself is filed.</summary>
        public const string TypeKey = "$type";

        private static readonly Regex _typeDeclaration = new Regex(@"\b(?:class|struct)\s+(\w+)");

        private static readonly Regex _memberDeclaration = new Regex(
            @"^\s*(?:\[[^\]]*\]\s*)*(?:(?:public|private|protected|internal|static|readonly|virtual|override|abstract|sealed|new|const)\s+)*[\w<>,\[\]\.\?]+\s+(\w+)\s*(?:\{|=|;)");

        private static readonly Regex _seeReference = new Regex(@"<see\s+cref\s*=\s*""(?:[\w\.]*\.)?(\w+)""\s*/?>");
        private static readonly Regex _summary = new Regex(@"<summary>(.*?)</summary>", RegexOptions.Singleline);
        private static readonly Regex _anyTag = new Regex(@"<[^>]+>");
        private static readonly Regex _whitespace = new Regex(@"\s+");

        public Dictionary<string, string> Parse(string source)
        {
            var help = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(source))
                return help;

            var comment = new StringBuilder();
            string[] lines = source.Replace("\r\n", "\n").Split('\n');

            foreach (string line in lines)
            {
                string trimmed = line.Trim();

                if (trimmed.StartsWith("///"))
                {
                    comment.Append(trimmed.Substring(3).Trim()).Append(' ');
                    continue;
                }

                if (comment.Length == 0)
                    continue;

                if (trimmed.Length == 0)
                {
                    comment.Clear();
                    continue;
                }

                string key = KeyFor(trimmed);

                if (key != null)
                {
                    string text = Clean(comment.ToString());

                    if (text.Length > 0 && !help.ContainsKey(key))
                        help[key] = text;
                }

                comment.Clear();
            }

            return help;
        }

        /// <summary>
        /// What the declaration under a comment is called. A type answers with the shared type
        /// key so a component's own summary has somewhere to live; anything the member pattern
        /// does not recognise - a method, a brace - answers null and the comment is dropped.
        /// </summary>
        private string KeyFor(string declaration)
        {
            Match type = _typeDeclaration.Match(declaration);

            if (type.Success)
                return TypeKey;

            Match member = _memberDeclaration.Match(declaration);

            return member.Success ? member.Groups[1].Value : null;
        }

        private string Clean(string comment)
        {
            Match summary = _summary.Match(comment);
            string body = summary.Success ? summary.Groups[1].Value : comment;

            body = _seeReference.Replace(body, "$1");
            body = body.Replace("<para>", " ").Replace("</para>", " ");
            body = _anyTag.Replace(body, string.Empty);

            return _whitespace.Replace(body, " ").Trim();
        }
    }
}

#endif
