#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration
{
    /// <summary>
    /// The defineConstraints of an asmdef, read off its text the way AssemblyDefinitionReferences
    /// reads the references: the key, then the quoted names inside the brackets that follow it.
    /// An asmdef is JSON Unity wrote, so the span between the key's brackets holds only those
    /// names, and nothing else in the file - the versionDefines block names the same define -
    /// is inside it.
    ///
    /// Each entry comes back as written, "!X" and "A || B" included: it is Unity's own syntax and
    /// the caller quotes it back to the reader rather than evaluating it.
    /// </summary>
    internal class AssemblyDefinitionDefineConstraints
    {
        private const string CONSTRAINTS_KEY = "\"defineConstraints\"";

        private static readonly Regex Entry = new Regex("\"(?<name>[^\"]+)\"", RegexOptions.Compiled);

        internal IReadOnlyList<string> Read(string asmdefContent)
        {
            var constraints = new List<string>();

            if (string.IsNullOrEmpty(asmdefContent)) return constraints;

            int keyIndex = asmdefContent.IndexOf(CONSTRAINTS_KEY, StringComparison.Ordinal);
            if (keyIndex < 0) return constraints;

            int openIndex = asmdefContent.IndexOf('[', keyIndex);
            int closeIndex = openIndex < 0 ? -1 : asmdefContent.IndexOf(']', openIndex);

            if (openIndex < 0 || closeIndex < 0) return constraints;

            string inner = asmdefContent.Substring(openIndex + 1, closeIndex - openIndex - 1);

            foreach (Match match in Entry.Matches(inner))
                constraints.Add(match.Groups["name"].Value);

            return constraints;
        }
    }
}
#endif
