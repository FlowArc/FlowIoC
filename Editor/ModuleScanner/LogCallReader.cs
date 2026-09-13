#if UNITY_EDITOR

using System;
using System.Collections.Generic;

namespace FlowIoC.Editor.ModuleScanner
{
    /// <summary>
    /// One argument of a call as it sits in the source: where it starts, where it ends, and the
    /// text between, trimmed of the whitespace around it.
    /// </summary>
    internal readonly struct ArgumentSpanEVO
    {
        internal int Start { get; }
        internal int End { get; }
        internal string Text { get; }

        internal ArgumentSpanEVO(int start, int end, string text)
        {
            Start = start;
            End = end;
            Text = text;
        }
    }

    /// <summary>
    /// One <c>FlowLogger.Log*(...)</c> call found in a source file: which method, where the call
    /// begins and ends, the line it is on, and its arguments split at the commas that belong to
    /// the call itself.
    /// </summary>
    internal class LogCallEVO
    {
        internal string Method { get; }
        internal int Start { get; }
        internal int End { get; }
        internal int Line { get; }
        internal IReadOnlyList<ArgumentSpanEVO> Arguments { get; }

        internal LogCallEVO(string method, int start, int end, int line, IReadOnlyList<ArgumentSpanEVO> arguments)
        {
            Method = method;
            Start = start;
            End = end;
            Line = line;
            Arguments = arguments;
        }
    }

    /// <summary>
    /// Finds every <c>FlowLogger.Log</c>, <c>LogWarning</c>, <c>LogError</c> and <c>LogLong</c>
    /// call in a C# source text and hands back its arguments as spans, so a check can read the
    /// first one and a repair can cut it out without disturbing the rest of the line.
    ///
    /// It reads the text the way the compiler tokenises it rather than with a regular expression,
    /// because a log message is exactly where the difficult tokens live: a comma inside the
    /// message, a quote inside an interpolation hole, a <c>)</c> in a verbatim string, a call
    /// commented out with <c>//</c>. A call inside a comment or a string is not a call and is not
    /// returned; an argument is split only at a comma that sits at the call's own depth, outside
    /// every literal and every nested pair of brackets.
    /// </summary>
    internal class LogCallReader
    {
        private const string RECEIVER = "FlowLogger.";

        // Longest first, so "LogWarning" is not read as "Log" followed by "Warning".
        private static readonly string[] Methods = {"LogWarning", "LogError", "LogLong", "Log"};

        internal IReadOnlyList<LogCallEVO> Read(string text)
        {
            var calls = new List<LogCallEVO>();
            if (string.IsNullOrEmpty(text)) return calls;

            int index = 0;

            while (index < text.Length)
            {
                int skipped = SkipNonCode(text, index);

                if (skipped != index)
                {
                    index = skipped;
                    continue;
                }

                if (text[index] == 'F' && IsWordStart(text, index) && TryReadCall(text, index, out LogCallEVO call))
                {
                    calls.Add(call);
                    index = call.End;
                    continue;
                }

                index++;
            }

            return calls;
        }

        private static bool TryReadCall(string text, int start, out LogCallEVO call)
        {
            call = null;

            if (!Matches(text, start, RECEIVER)) return false;

            int nameStart = start + RECEIVER.Length;
            string method = null;

            foreach (string candidate in Methods)
            {
                if (Matches(text, nameStart, candidate) && !IsIdentifierChar(Peek(text, nameStart + candidate.Length)))
                {
                    method = candidate;
                    break;
                }
            }

            if (method == null) return false;

            int open = SkipWhitespace(text, nameStart + method.Length);
            if (Peek(text, open) != '(') return false;

            if (!TryReadArguments(text, open + 1, out List<ArgumentSpanEVO> arguments, out int end))
                return false;

            call = new LogCallEVO(method, start, end, LineOf(text, start), arguments);
            return true;
        }

        /// <summary>
        /// Reads from just after the opening parenthesis to the one that closes it. A comma at
        /// the call's own depth ends an argument; brackets of every kind nest, and a literal or a
        /// comment is stepped over whole.
        /// </summary>
        private static bool TryReadArguments(string text, int from, out List<ArgumentSpanEVO> arguments, out int end)
        {
            arguments = new List<ArgumentSpanEVO>();
            end = from;

            int depth = 0;
            int argumentStart = from;
            int index = from;

            while (index < text.Length)
            {
                int skipped = SkipNonCode(text, index);

                if (skipped != index)
                {
                    index = skipped;
                    continue;
                }

                char c = text[index];

                if (c == '(' || c == '[' || c == '{')
                {
                    depth++;
                }
                else if (c == ')' || c == ']' || c == '}')
                {
                    if (depth == 0)
                    {
                        if (c != ')') return false;

                        AddArgument(text, argumentStart, index, arguments);
                        end = index + 1;
                        return true;
                    }

                    depth--;
                }
                else if (c == ',' && depth == 0)
                {
                    AddArgument(text, argumentStart, index, arguments);
                    argumentStart = index + 1;
                }

                index++;
            }

            return false;
        }

        private static void AddArgument(string text, int from, int to, List<ArgumentSpanEVO> arguments)
        {
            int start = from;
            int end = to;

            while (start < end && char.IsWhiteSpace(text[start])) start++;
            while (end > start && char.IsWhiteSpace(text[end - 1])) end--;

            // The empty span between "(" and ")" is no argument at all.
            if (start == end && arguments.Count == 0) return;

            arguments.Add(new ArgumentSpanEVO(start, end, text.Substring(start, end - start)));
        }

        /// <summary>
        /// The index just past whatever non-code sits at <paramref name="index"/> - a comment, a
        /// string literal in any of its spellings, a character literal - or <paramref name="index"/>
        /// itself when code sits there.
        /// </summary>
        private static int SkipNonCode(string text, int index)
        {
            char c = text[index];
            char next = Peek(text, index + 1);

            if (c == '/' && next == '/') return SkipLineComment(text, index);
            if (c == '/' && next == '*') return SkipBlockComment(text, index);
            if (c == '\'') return SkipCharLiteral(text, index);

            if (TryStringStart(text, index, out bool verbatim, out bool interpolated, out int quote))
                return SkipString(text, quote, verbatim, interpolated);

            return index;
        }

        private static int SkipLineComment(string text, int index)
        {
            while (index < text.Length && text[index] != '\n') index++;
            return index;
        }

        private static int SkipBlockComment(string text, int index)
        {
            int close = text.IndexOf("*/", index + 2, StringComparison.Ordinal);
            return close < 0 ? text.Length : close + 2;
        }

        private static int SkipCharLiteral(string text, int index)
        {
            int cursor = index + 1;
            if (cursor >= text.Length) return cursor;

            cursor += text[cursor] == '\\' ? 2 : 1;

            // A lone apostrophe that is no character literal is stepped over as one character.
            return cursor < text.Length && text[cursor] == '\'' ? cursor + 1 : index + 1;
        }

        /// <summary>
        /// Whether a string literal starts here, in any spelling: <c>"</c>, <c>@"</c>, <c>$"</c>,
        /// <c>$@"</c> or <c>@$"</c>. <paramref name="quote"/> is where the opening quote sits.
        /// </summary>
        private static bool TryStringStart(string text, int index, out bool verbatim, out bool interpolated, out int quote)
        {
            verbatim = false;
            interpolated = false;
            quote = index;

            while (quote < text.Length && (text[quote] == '@' || text[quote] == '$'))
            {
                if (text[quote] == '@') verbatim = true;
                else interpolated = true;

                quote++;
            }

            if (quote >= text.Length || text[quote] != '"') return false;

            // "@" and "$" are prefixes only when they touch the quote; "@name" is an identifier.
            return quote - index <= 2;
        }

        /// <summary>
        /// From the opening quote to just past the closing one. A regular string ends at the first
        /// quote not escaped by a backslash; a verbatim string ends at the first quote not doubled;
        /// an interpolated string steps over each hole whole, strings inside it included, because
        /// a quote inside a hole is a string of its own rather than the end of this one.
        /// </summary>
        private static int SkipString(string text, int quote, bool verbatim, bool interpolated)
        {
            int index = quote + 1;

            while (index < text.Length)
            {
                char c = text[index];

                if (c == '"')
                {
                    if (verbatim && Peek(text, index + 1) == '"')
                    {
                        index += 2;
                        continue;
                    }

                    return index + 1;
                }

                if (!verbatim && c == '\\')
                {
                    index += 2;
                    continue;
                }

                if (!verbatim && c == '\n') return index;

                if (interpolated && c == '{')
                {
                    if (Peek(text, index + 1) == '{')
                    {
                        index += 2;
                        continue;
                    }

                    index = SkipInterpolationHole(text, index + 1);
                    continue;
                }

                index++;
            }

            return text.Length;
        }

        /// <summary>
        /// From just after a hole's opening brace to just past the brace that closes it. Whatever
        /// sits inside is code: strings, character literals and nested braces are read as such.
        /// </summary>
        private static int SkipInterpolationHole(string text, int index)
        {
            int depth = 1;

            while (index < text.Length)
            {
                int skipped = SkipNonCode(text, index);

                if (skipped != index)
                {
                    index = skipped;
                    continue;
                }

                char c = text[index];

                if (c == '{') depth++;
                else if (c == '}' && --depth == 0) return index + 1;

                index++;
            }

            return text.Length;
        }

        private static int SkipWhitespace(string text, int index)
        {
            while (index < text.Length && char.IsWhiteSpace(text[index])) index++;
            return index;
        }

        private static int LineOf(string text, int index)
        {
            int line = 1;
            for (int cursor = 0; cursor < index; cursor++)
                if (text[cursor] == '\n') line++;

            return line;
        }

        private static bool Matches(string text, int index, string expected) =>
            index + expected.Length <= text.Length
            && string.CompareOrdinal(text, index, expected, 0, expected.Length) == 0;

        // "MyFlowLogger.Log" is another type; "FlowIoC.ConsoleModule.FlowLogger.Log" is this one.
        private static bool IsWordStart(string text, int index) =>
            index == 0 || !IsIdentifierChar(text[index - 1]);

        private static bool IsIdentifierChar(char c) => char.IsLetterOrDigit(c) || c == '_';

        private static char Peek(string text, int index) => index < text.Length ? text[index] : '\0';
    }
}

#endif
