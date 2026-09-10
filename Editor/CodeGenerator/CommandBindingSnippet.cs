#if UNITY_EDITOR
using System.IO;
using System.Text.RegularExpressions;

namespace FlowIoC.Editor.CodeGenerator
{
    /// <summary>
    /// The binding a Context needs for the command Create Command writes, shown beside the
    /// command rather than written for you. Where a command sits in a sequence is a decision
    /// about the flow, and the window used to take it - appending to whatever block bound the
    /// signal, or opening a new one - from two names typed as text that nothing checked. It now
    /// hands over the two lines, spelled against the holder field the Context declares, and the
    /// author pastes them where the flow reads right.
    /// </summary>
    internal class CommandBindingSnippet
    {
        /// <summary>
        /// The field a Context is expected to hold its public signal holder in, for a Context
        /// that cannot be read or declares none.
        /// </summary>
        private const string CONVENTIONAL_FIELD = "_signals";

        /// <summary>
        /// The two lines. The signal is the one named after the command on the holder's
        /// Incoming, which is the name a command answers to when the module follows the
        /// convention; a command bound to something else is edited after the paste.
        /// </summary>
        internal string For(string commandName, string holderField)
        {
            return "CommandBinder.Bind(" + holderField + ".Incoming." + commandName + ")\n"
                   + "    .ToSequence<" + commandName + "Command>();";
        }

        /// <summary>
        /// The field the Context declares its public holder in, read off the declaration: the
        /// first field whose type ends in Signals and is not the module's InternalSignals, whose
        /// halves are the module's own and carry no Incoming. The conventional name when there is
        /// no file to read or no such field in it.
        /// </summary>
        internal string HolderFieldIn(string contextPath)
        {
            if (string.IsNullOrEmpty(contextPath) || !File.Exists(contextPath)) return CONVENTIONAL_FIELD;

            foreach (string line in File.ReadAllLines(contextPath))
            {
                Match match = Regex.Match(line, @"\b(\w+Signals)\s+(_\w+)\s*(;|=)");

                if (!match.Success || match.Groups[1].Value.EndsWith("InternalSignals")) continue;

                return match.Groups[2].Value;
            }

            return CONVENTIONAL_FIELD;
        }
    }
}

#endif
