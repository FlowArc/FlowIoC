#if UNITY_EDITOR
using System.Collections.Generic;

namespace FlowIoC.Editor.ModuleScanner
{
    /// <summary>
    /// What one Fix All run did. Remaining is the honest half: the findings the repair was not
    /// allowed to touch, and the ones whose repair failed, each already named with the module it
    /// belongs to so the summary reads on its own after a domain reload has taken the panel's
    /// state with it.
    /// </summary>
    internal class RepairResultEVO
    {
        internal int Fixed { get; set; }
        internal List<string> Remaining { get; } = new List<string>();

        internal string Summary =>
            Remaining.Count == 0
                ? $"{Fixed} fixed."
                : $"{Fixed} fixed, {Remaining.Count} need a person.";
    }
}

#endif
