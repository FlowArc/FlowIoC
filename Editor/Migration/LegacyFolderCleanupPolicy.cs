#if UNITY_EDITOR

namespace FlowIoC.Editor.Migration
{
    /// <summary>
    /// Decides whether a legacy folder may be removed after the migration. Assets/Editor and
    /// Assets/Resources are shared with the game, so FlowIoC only removes a folder when its own
    /// files were the last thing left in it.
    /// </summary>
    internal class LegacyFolderCleanupPolicy
    {
        internal bool ShouldDelete(bool exists, bool isEmpty)
        {
            return exists && isEmpty;
        }
    }
}

#endif
