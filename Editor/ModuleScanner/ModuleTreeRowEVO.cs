#if UNITY_EDITOR

namespace FlowIoC.Editor.ModuleScanner
{
    /// <summary>
    /// One row of the panel in the place the tree puts it: the module's row, how deep inside
    /// other modules it sits, and the entry it hangs from.
    /// </summary>
    internal class ModuleTreeRowEVO
    {
        internal ModuleRowEVO Row { get; set; }

        /// <summary>How many modules this one sits inside. A top level module is at zero.</summary>
        internal int Depth { get; set; }

        /// <summary>
        /// The entry of the module this one lives in, or null at the top. The guide line a row is
        /// drawn with runs from that entry's arrow down to this row, so the window needs the
        /// entry rather than the name - it is the entry's rect it draws from.
        /// </summary>
        internal ModuleTreeRowEVO Parent { get; set; }

        /// <summary>
        /// Whether this row, or anything under it, has more than Ok to say. "Only issues" keeps a
        /// row that is green itself when something under it is not, so that the row with the
        /// issue still has a parent to hang from.
        /// </summary>
        internal bool HasIssue { get; set; }
    }
}

#endif
