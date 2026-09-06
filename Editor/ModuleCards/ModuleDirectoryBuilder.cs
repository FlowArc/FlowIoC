#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Text;

namespace FlowIoC.Editor.ModuleCards
{
    /// <summary>
    /// Every card's purpose and concepts, in one list an agent reads before it opens anything.
    /// This is the whole point of the feature: finding which module a piece of work belongs to
    /// is the largest single cost of working in a project with thirty of them.
    ///
    /// The warning line is inside the generated body rather than above it, so that anybody who
    /// commits this file despite the ignore rule puts the warning in the diff.
    /// </summary>
    internal class ModuleDirectoryBuilder
    {
        internal const string WARNING = "This file is generated and gitignored. Do not commit it.";

        private const string NO_PURPOSE = "_no purpose written yet_";

        internal string Build(IReadOnlyList<ModuleCardEntryEVO> entries)
        {
            var builder = new StringBuilder();
            builder.Append(WARNING).Append('\n');

            string group = null;

            foreach (ModuleCardEntryEVO entry in entries ?? new List<ModuleCardEntryEVO>())
            {
                if (!string.Equals(group, entry.Group, StringComparison.Ordinal))
                {
                    group = entry.Group;
                    builder.Append('\n').Append("### ").Append(group).Append('\n');
                }

                string indent = new string(' ', entry.Depth * 2);
                string purpose = string.IsNullOrEmpty(entry.Purpose) ? NO_PURPOSE : entry.Purpose;

                builder.Append(indent)
                    .Append("- **").Append(entry.Name).Append("** (").Append(entry.Kind).Append(") — ")
                    .Append(purpose).Append('\n');

                builder.Append(indent).Append("  `").Append(entry.RelativePath).Append('`');

                if (!string.IsNullOrEmpty(entry.Concepts))
                    builder.Append(" · ").Append(entry.Concepts);

                builder.Append('\n');
            }

            return builder.ToString().TrimEnd('\n');
        }
    }
}

#endif
