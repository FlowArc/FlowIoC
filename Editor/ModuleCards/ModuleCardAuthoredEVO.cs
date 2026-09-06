#if UNITY_EDITOR

namespace FlowIoC.Editor.ModuleCards
{
    /// <summary>
    /// The hand-written half of one MODULE.md, as the tools need it. Only the two lines the
    /// directory quotes are parsed out; Decisions and Known gaps are for whoever opens the card
    /// and nothing reads them.
    /// </summary>
    internal class ModuleCardAuthoredEVO
    {
        internal string Purpose { get; set; }
        internal string Concepts { get; set; }
        internal bool PurposeIsPlaceholder { get; set; }
        internal bool ConceptsIsPlaceholder { get; set; }
    }
}

#endif
