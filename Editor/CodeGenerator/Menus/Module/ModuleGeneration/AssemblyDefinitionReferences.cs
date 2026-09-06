#if UNITY_EDITOR
using System;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration
{
    /// <summary>
    /// Adds one entry to an existing asmdef's reference list.
    ///
    /// <see cref="AssemblyDefinitionTemplate"/> writes a whole file and is right when the module is
    /// being created. Adding Shared to a module that already exists is the opposite case: the
    /// asmdef may carry references someone added by hand - a Service module, a Unity package - and
    /// rewriting it from the template would silently drop them. So this edits the reference block
    /// in place and leaves every other byte of the file alone.
    /// </summary>
    internal class AssemblyDefinitionReferences
    {
        private const string REFERENCES_KEY = "\"references\"";
        private const string DEFAULT_INDENT = "    ";

        /// <summary>
        /// Returns the asmdef text with <paramref name="referenceAssembly"/> added, or the text
        /// unchanged when it is already listed. <paramref name="added"/> says which happened, so a
        /// caller can skip writing a file it would not change - and report honestly about a module
        /// that was already wired.
        /// </summary>
        public string Add(string asmdefContent, string referenceAssembly, out bool added)
        {
            added = false;

            if (string.IsNullOrEmpty(asmdefContent) || string.IsNullOrEmpty(referenceAssembly)) return asmdefContent;

            int keyIndex = asmdefContent.IndexOf(REFERENCES_KEY, StringComparison.Ordinal);
            if (keyIndex < 0) return asmdefContent;

            int openIndex = asmdefContent.IndexOf('[', keyIndex);
            if (openIndex < 0) return asmdefContent;

            int closeIndex = asmdefContent.IndexOf(']', openIndex);
            if (closeIndex < 0) return asmdefContent;

            string inner = asmdefContent.Substring(openIndex + 1, closeIndex - openIndex - 1);
            string quoted = "\"" + referenceAssembly + "\"";

            if (inner.Contains(quoted)) return asmdefContent;

            added = true;

            string replacement = inner.Trim().Length == 0
                ? WriteOnlyEntry(quoted, asmdefContent, closeIndex)
                : AppendEntry(inner, quoted);

            return asmdefContent.Substring(0, openIndex + 1) + replacement + asmdefContent.Substring(closeIndex);
        }

        /// <summary>
        /// Returns the asmdef text with <paramref name="referenceAssembly"/> taken out, or the
        /// text unchanged when it was not listed. <paramref name="removed"/> says which happened.
        ///
        /// This is what a module being deleted needs of every asmdef that named it. Left in, the
        /// reference points at an assembly that no longer exists and Unity refuses to compile the
        /// module holding it, with an error about the asmdef rather than about the code. Taken
        /// out, a module that only ever held the reference compiles cleanly, and a module that
        /// actually used the deleted types says so on the line that used them.
        ///
        /// Only the entry is removed. Every other byte of the file is left alone, for the same
        /// reason <see cref="Add"/> edits in place: an asmdef carries references somebody added by
        /// hand, and rewriting it from a template would drop them.
        /// </summary>
        public string Remove(string asmdefContent, string referenceAssembly, out bool removed)
        {
            removed = false;

            if (string.IsNullOrEmpty(asmdefContent) || string.IsNullOrEmpty(referenceAssembly)) return asmdefContent;

            int keyIndex = asmdefContent.IndexOf(REFERENCES_KEY, StringComparison.Ordinal);
            if (keyIndex < 0) return asmdefContent;

            int openIndex = asmdefContent.IndexOf('[', keyIndex);
            if (openIndex < 0) return asmdefContent;

            int closeIndex = asmdefContent.IndexOf(']', openIndex);
            if (closeIndex < 0) return asmdefContent;

            string inner = asmdefContent.Substring(openIndex + 1, closeIndex - openIndex - 1);
            string quoted = "\"" + referenceAssembly + "\"";

            int entryIndex = inner.IndexOf(quoted, StringComparison.Ordinal);
            if (entryIndex < 0) return asmdefContent;

            removed = true;

            string trimmed = WithoutEntry(inner, entryIndex, quoted.Length);

            return asmdefContent.Substring(0, openIndex + 1) + trimmed + asmdefContent.Substring(closeIndex);
        }

        /// <summary>
        /// The entry and the comma that joined it to its neighbour - the one before it where there
        /// is one, so the last entry in a list does not leave a trailing comma behind, and the one
        /// after it otherwise. A list left holding nothing comes back empty rather than as a line
        /// of whitespace.
        /// </summary>
        private string WithoutEntry(string inner, int entryIndex, int entryLength)
        {
            int start = entryIndex;
            int end = entryIndex + entryLength;

            int commaBefore = inner.LastIndexOf(',', Math.Max(start - 1, 0));
            bool hasEntryBefore = commaBefore >= 0 && inner.Substring(0, start).Contains("\"");

            if (hasEntryBefore && commaBefore < start)
            {
                start = commaBefore;
            }
            else
            {
                int commaAfter = inner.IndexOf(',', end);
                if (commaAfter >= 0) end = commaAfter + 1;

                // The first entry owns the line break and indent that opened it. Taking the entry
                // and the comma after it but leaving those behind is what turns a tidy list into
                // one with a blank line at the top of it.
                while (start > 0 && char.IsWhiteSpace(inner[start - 1])) start--;
            }

            string result = inner.Remove(start, end - start);

            return result.Trim().Length == 0 ? string.Empty : result;
        }

        /// <summary>
        /// An empty list has no sibling to copy an indent from, so the closing bracket's own line
        /// is what the new entry is lined up against.
        /// </summary>
        private string WriteOnlyEntry(string quoted, string asmdefContent, int closeIndex)
        {
            string closingIndent = IndentOfLineAt(asmdefContent, closeIndex);

            return "\n" + closingIndent + DEFAULT_INDENT + quoted + "\n" + closingIndent;
        }

        /// <summary>
        /// The new entry goes after the last one and copies its indent, so a file written with tabs
        /// stays written with tabs.
        /// </summary>
        private string AppendEntry(string inner, string quoted)
        {
            int lastQuote = inner.LastIndexOf('"');
            string head = inner.Substring(0, lastQuote + 1);
            string tail = inner.Substring(lastQuote + 1);

            return head + ",\n" + IndentOfLastEntry(inner) + quoted + tail;
        }

        private string IndentOfLastEntry(string inner)
        {
            int lastQuote = inner.LastIndexOf('"');
            int entryStart = inner.LastIndexOf('"', Math.Max(lastQuote - 1, 0));

            return IndentOfLineAt(inner, entryStart);
        }

        /// <summary>
        /// The whitespace that opens the line <paramref name="index"/> falls on. Anything other than
        /// leading whitespace ends it, so a line that does not start with its entry - a one-line
        /// array, say - contributes nothing rather than a wrong indent.
        /// </summary>
        private string IndentOfLineAt(string text, int index)
        {
            int lineStart = text.LastIndexOf('\n', Math.Min(Math.Max(index, 0), text.Length - 1));
            if (lineStart < 0) return DEFAULT_INDENT;

            var indent = string.Empty;

            for (int i = lineStart + 1; i < index && i < text.Length; i++)
            {
                if (!char.IsWhiteSpace(text[i]) || text[i] == '\n') return indent;

                indent += text[i];
            }

            return indent;
        }
    }
}
#endif