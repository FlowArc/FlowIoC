#if UNITY_EDITOR

using System;
using System.Collections.Generic;

namespace FlowIoC.Editor.ModuleInstall
{
    /// <summary>One verdict per file, and the counts the dialog reads off them.</summary>
    internal class ModuleUpdatePlanEVO
    {
        internal SortedDictionary<string, ModuleUpdateVerdict> Verdicts { get; } =
            new SortedDictionary<string, ModuleUpdateVerdict>(StringComparer.Ordinal);

        /// <summary>
        /// Whether the installed copy kept a record. Without one every difference is a conflict,
        /// and the dialog says why.
        /// </summary>
        internal bool HadRecord { get; set; }

        internal int Count(ModuleUpdateVerdict verdict)
        {
            var count = 0;

            foreach (ModuleUpdateVerdict value in Verdicts.Values)
            {
                if (value == verdict)
                    count++;
            }

            return count;
        }

        internal IReadOnlyList<string> Of(ModuleUpdateVerdict verdict)
        {
            var paths = new List<string>();

            foreach (KeyValuePair<string, ModuleUpdateVerdict> pair in Verdicts)
            {
                if (pair.Value == verdict)
                    paths.Add(pair.Key);
            }

            return paths;
        }

        internal bool HasConflicts => Count(ModuleUpdateVerdict.Conflict) > 0;

        /// <summary>Nothing to write and nothing to delete: the two copies already agree.</summary>
        internal bool ChangesNothing =>
            Count(ModuleUpdateVerdict.Overwrite) == 0
            && Count(ModuleUpdateVerdict.Copy) == 0
            && Count(ModuleUpdateVerdict.Delete) == 0
            && Count(ModuleUpdateVerdict.Conflict) == 0;
    }
}

#endif
