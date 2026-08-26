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
