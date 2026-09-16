#if UNITY_EDITOR

namespace FlowIoC.Editor.ModuleInstall
{
    /// <summary>
    /// What an update does with one file. "The package" is the shipped record against the
    /// installed record; "the game" is the installed file against the installed record.
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

        /// <summary>The package removed it and the game edited it. Stays; listed in the dialog.</summary>
        KeepRemoved = 5,

        /// <summary>Both changed it, or the package added it over a file the game already had.</summary>
        Conflict = 6,

        /// <summary>A generated part. Always written; the rescan rewrites it anyway.</summary>
        Regenerate = 7
    }
}

#endif
