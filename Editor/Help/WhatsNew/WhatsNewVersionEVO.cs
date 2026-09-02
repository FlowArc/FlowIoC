#if UNITY_EDITOR
using System.Collections.Generic;

namespace FlowIoC.Editor.Help.WhatsNew
{
    /// <summary>
    /// One release as the What's New tab shows it: what it is called, when it went out, and the
    /// sections it was written with.
    /// </summary>
    internal class WhatsNewVersionEVO
    {
        internal string Version { get; set; }

        /// <summary>Empty for a section that carries no date, which is what Unreleased is.</summary>
        internal string Date { get; set; }

        internal IReadOnlyList<WhatsNewGroupEVO> Groups { get; set; } = new List<WhatsNewGroupEVO>();
    }
}

#endif
