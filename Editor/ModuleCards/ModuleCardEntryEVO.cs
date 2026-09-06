#if UNITY_EDITOR

using FlowIoC.Editor.Modules;

namespace FlowIoC.Editor.ModuleCards
{
    /// <summary>
    /// One line of the directory. Depth is carried rather than computed from the path, because
    /// the builder must not have to know how a screen module is nested to indent it.
    /// </summary>
    internal class ModuleCardEntryEVO
    {
        internal string Name { get; set; }
        internal ModuleKind Kind { get; set; }

        /// <summary>The card root this module was found under, relative to the project root.</summary>
        internal string Group { get; set; }

        /// <summary>The module folder, relative to the project root, with forward slashes.</summary>
        internal string RelativePath { get; set; }

        internal int Depth { get; set; }

        /// <summary>
        /// Whether the module has a MODULE.md at all. A module with no card and one whose purpose
        /// is still the stub both leave <see cref="Purpose"/> null, and the panel has to tell them
        /// apart: the first is a file FlowIoC writes, the second is a sentence only a person can.
        /// </summary>
        internal bool HasCard { get; set; }

        /// <summary>
        /// Whether Module Scanner has a target for this module, and so whether Fix All can write
        /// its card. The framework's own modules under a package's Runtime folder are not scanner
        /// targets: their cards are written by hand, and reporting one as a file FlowIoC would
        /// write leaves the Agent Scanner's Sync button lit by something pressing it cannot fix.
        /// </summary>
        internal bool ScannerOwned { get; set; }

        internal string Purpose { get; set; }
        internal string Concepts { get; set; }
    }
}

#endif