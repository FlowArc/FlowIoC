#if UNITY_EDITOR
using System.Collections.Generic;

namespace FlowIoC.Editor.Help.WhatsNew
{
    /// <summary>
    /// One section of a release - Added, Changed, Fixed, Removed - and the headlines under it.
    /// The title is whatever the changelog wrote, so a release that invents a section of its own
    /// still reads.
    /// </summary>
    internal class WhatsNewGroupEVO
    {
        internal string Title { get; set; }

        internal IReadOnlyList<string> Lines { get; set; } = new List<string>();
    }
}

#endif
