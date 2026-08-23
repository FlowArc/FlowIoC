#if UNITY_EDITOR

namespace FlowIoC.Editor.Migration
{
    /// <summary>
    /// Decides whether a legacy FlowIoC asset may be moved to its new home. An occupied
    /// destination means the project already has a copy there; overwriting it would throw away
    /// whichever copy holds the user's real settings, so the legacy file is left in place and the
    /// migrator warns instead.
    /// </summary>
    internal class LegacyAssetMovePolicy
    {
        internal bool ShouldMove(bool legacyExists, bool destinationExists)
        {
            return legacyExists && !destinationExists;
        }
    }
}

#endif
