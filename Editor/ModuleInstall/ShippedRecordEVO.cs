#if UNITY_EDITOR

using System;
using System.Collections.Generic;

namespace FlowIoC.Editor.ModuleInstall
{
    /// <summary>
    /// What the package shipped, one hash per file, keyed by the path relative to the module
    /// folder with forward slashes. Sorted, so the file it is written to reads the same on every
    /// machine and a diff of it says what changed.
    /// </summary>
    internal class ShippedRecordEVO
    {
        internal SortedDictionary<string, string> Files { get; } =
            new SortedDictionary<string, string>(StringComparer.Ordinal);
    }
}

#endif
