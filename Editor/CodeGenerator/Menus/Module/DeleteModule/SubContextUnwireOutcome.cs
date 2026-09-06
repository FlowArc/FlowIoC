#if UNITY_EDITOR

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.DeleteModule
{
    /// <summary>
    /// What happened to one sub-context entry Delete Module found.
    ///
    /// Every value carries its number, because Unity serializes an enum as an int and a value
    /// inserted in the middle would silently renumber the ones below it.
    /// </summary>
    internal enum SubContextUnwireOutcome
    {
        /// <summary>Found and reported, nothing done to it. This is what cancelling leaves.</summary>
        Found = 0,

        /// <summary>Taken out, and the asset holding it written.</summary>
        Removed = 1,

        /// <summary>
        /// Taken out of a scene that is open, which is therefore left dirty and unsaved. Saving
        /// somebody's open scene would destroy whatever they had not saved in it yet, so the
        /// removal is made and the saving is theirs.
        /// </summary>
        RemovedNotSaved = 2,

        /// <summary>Offered and declined, one entry at a time.</summary>
        Skipped = 3
    }
}

#endif
