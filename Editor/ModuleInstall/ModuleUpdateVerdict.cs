#if UNITY_EDITOR

namespace FlowIoC.Editor.ModuleInstall
{
    /// <summary>
    /// What an update does with one file. "The package" is the shipped record against the
    /// installed record; "the game" is the installed file against the installed record.
    ///
    /// Code and data part where both sides changed a file. Code - scripts, assembly files, the
    /// card - is the package's, and its version replaces the game's edit, listed beforehand. Data
    /// - an asset, a prefab, a scene, art - may be the game's work, so the reader is asked about
    /// each file.
    /// </summary>
    internal enum ModuleUpdateVerdict
    {
        /// <summary>The package did not change it. Whatever the game did stays, silently.</summary>
        Leave = 0,

        /// <summary>The package changed it and the game did not.</summary>
        Overwrite = 1,

        /// <summary>The package added it and the game has nothing there.</summary>
        Copy = 2,

        /// <summary>The package removed it and the game left it alone.</summary>
        Delete = 3,

        /// <summary>The package did not change it and the game did. Stays; counted for the dialog.</summary>
        KeepEdited = 4,

        // 5 was KeepRemoved: a removed file the game had edited stayed unasked. Code now goes as
        // RemoveEdited and data is asked as a Conflict. Never reuse the number.

        /// <summary>
        /// Data both changed, data the package removed and the game edited, or a file the package
        /// added over one the game already had. The reader chooses for each.
        /// </summary>
        Conflict = 6,

        /// <summary>A generated part. Always written; the rescan rewrites it anyway.</summary>
        Regenerate = 7,

        /// <summary>Code both changed. The package's version replaces the game's edit; listed first.</summary>
        Replace = 8,

        /// <summary>Code the package removed and the game edited. Removed; listed first.</summary>
        RemoveEdited = 9,

        /// <summary>The meta of a conflicting file. It goes the way its file goes and is never asked about.</summary>
        WithAsset = 10
    }
}

#endif