#if UNITY_EDITOR

namespace FlowIoC.Editor.ModuleCards
{
    /// <summary>
    /// What a module's card says about itself before the module exists - the two lines Create
    /// Module offers to fill in while the author still has the answer in mind. Either may be left
    /// empty, and then the card is written with the stub's placeholders and Module Scanner asks
    /// for them later. Creating a module is never blocked on prose.
    /// </summary>
    internal class ModuleCardDraftEVO
    {
        internal string Purpose { get; set; }
        internal string Concepts { get; set; }

        internal bool IsEmpty =>
            string.IsNullOrWhiteSpace(Purpose) && string.IsNullOrWhiteSpace(Concepts);
    }
}

#endif
