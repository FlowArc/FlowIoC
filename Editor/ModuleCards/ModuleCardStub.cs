#if UNITY_EDITOR

namespace FlowIoC.Editor.ModuleCards
{
    /// <summary>
    /// The card a module is born with. Both lines the directory needs are written in italics,
    /// which is the convention for "not filled in yet": it renders as visibly provisional to a
    /// person, it is unambiguous to parse, and nobody writes a real purpose that way.
    ///
    /// Decisions and Known gaps are left out rather than stubbed. A module usually has neither on
    /// the day it is created, and an empty heading invites filler.
    /// </summary>
    internal class ModuleCardStub
    {
        internal const string PURPOSE_PLACEHOLDER = "_Say in one line what this module is for._";

        internal const string CONCEPTS_PLACEHOLDER =
            "_List the words someone would search for when the work belongs here._";

        internal string For(string moduleName) => For(moduleName, null);

        /// <summary>
        /// The same card, with whatever the author already answered written in. A line left empty
        /// keeps its placeholder, so a half-filled card still asks for the half that is missing.
        /// </summary>
        internal string For(string moduleName, ModuleCardDraftEVO draft)
        {
            return "# " + moduleName + "\n"
                   + "\n"
                   + "## Purpose\n"
                   + Line(draft?.Purpose, PURPOSE_PLACEHOLDER) + "\n"
                   + "\n"
                   + "## Concepts\n"
                   + Line(draft?.Concepts, CONCEPTS_PLACEHOLDER) + "\n";
        }

        private string Line(string written, string placeholder) =>
            string.IsNullOrWhiteSpace(written) ? placeholder : written.Trim();
    }
}

#endif