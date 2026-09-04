#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FlowIoC.Editor.CodeGenerator
{
    /// <summary>
    /// The `CommandBinder.Bind(...)` block a context holds for one signal, as text.
    ///
    /// A binding names how each command runs as it names the command - `ToSequence&lt;T&gt;()` or
    /// `ToParallel&lt;T&gt;()` - so a block is a signal and a list of commands, and nothing is
    /// appended after the last one. Blocks written before that API existed said `.To&lt;T&gt;()`
    /// and closed with `.InSequence()` or `.InParallel()`; those are read as well, and rewritten
    /// into what compiles.
    /// </summary>
    internal class CommandBindingBlock
    {
        private const string BODY_INDENT = "            ";
        private const string CHAIN_INDENT = "                ";

        private static readonly Regex CommandCall = new Regex(@"\.To(Sequence|Parallel)?<([^>]+)>\(\)");

        /// <summary>
        /// <paramref name="existingBlock"/> with <paramref name="commandName"/> added, or the block
        /// untouched when it binds another signal. A command the block already runs is not added
        /// twice, and the order the commands were in is kept.
        /// </summary>
        public string Merge(string existingBlock, string signal, string commandName, bool isSequence)
        {
            if (!existingBlock.Contains($"Bind({signal})"))
                return existingBlock;

            List<Call> calls = Read(existingBlock, isSequence);

            if (!string.IsNullOrEmpty(commandName) && !Contains(calls, commandName))
                calls.Add(new Call(commandName, isSequence));

            return calls.Count == 0 ? existingBlock : Write(signal, calls);
        }

        /// <summary>
        /// A block for a signal the context does not bind yet.
        /// </summary>
        public string Create(string signal, string commandName, bool isSequence)
        {
            if (string.IsNullOrEmpty(commandName))
                return $"{BODY_INDENT}CommandBinder.Bind({signal});";

            return Write(signal, new List<Call> {new Call(commandName, isSequence)});
        }

        /// <summary>
        /// The commands a block runs, in order. A legacy `.To&lt;T&gt;()` carries no mode of its
        /// own, so it takes the one the block closed with, and the caller's choice where the block
        /// closed with neither.
        /// </summary>
        private List<Call> Read(string block, bool isSequence)
        {
            bool legacyIsSequence = block.Contains(".InSequence()")
                                    || (!block.Contains(".InParallel()") && isSequence);

            var calls = new List<Call>();

            foreach (Match match in CommandCall.Matches(block))
            {
                string command = match.Groups[2].Value.Trim();

                if (Contains(calls, command)) continue;

                string mode = match.Groups[1].Value;

                calls.Add(new Call(command, mode.Length == 0 ? legacyIsSequence : mode == "Sequence"));
            }

            return calls;
        }

        private string Write(string signal, List<Call> calls)
        {
            var lines = new List<string> {$"{BODY_INDENT}CommandBinder.Bind({signal})"};

            foreach (Call call in calls)
                lines.Add($"{CHAIN_INDENT}.To{(call.IsSequence ? "Sequence" : "Parallel")}<{call.Command}>()");

            lines[lines.Count - 1] += ";";

            return string.Join("\r\n", lines);
        }

        private bool Contains(List<Call> calls, string command)
        {
            foreach (Call call in calls)
                if (call.Command == command)
                    return true;

            return false;
        }

        private readonly struct Call
        {
            public Call(string command, bool isSequence)
            {
                Command = command;
                IsSequence = isSequence;
            }

            public string Command { get; }
            public bool IsSequence { get; }
        }
    }
}
#endif
