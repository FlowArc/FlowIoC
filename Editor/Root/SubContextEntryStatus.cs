#if UNITY_EDITOR

namespace FlowIoC.Editor.Root
{
    /// <summary>
    /// What a Root's sub-context entry amounts to, and what the inspector draws it as.
    ///
    /// The order matters and is settled-to-worst, the way ModuleCheckStatus is, so a Root listing
    /// several entries can take the worst of them by comparing these values. Every value carries
    /// its number, because Unity serializes an enum as an int.
    /// </summary>
    internal enum SubContextEntryStatus
    {
        /// <summary>The entry holds its context's script, so the reference is tracked.</summary>
        Linked = 0,

        /// <summary>
        /// No script, but the name still names a context this project compiled - an entry written
        /// before the reference existed, or one somebody cleared. One press of Resolve mends it.
        /// </summary>
        Unlinked = 1,

        /// <summary>
        /// No script and nothing compiles to the name. The module that declared this context has
        /// been deleted or renamed, and the Root is listing something that will never be built.
        /// Nothing can mend this on its own: the entry is removed, or the context comes back.
        /// </summary>
        Unresolved = 2
    }
}

#endif
