#if UNITY_EDITOR

namespace Modules.AudioModule.Editor
{
    /// <summary>Where a module stands with sound: nothing yet, one of its two halves, or both.</summary>
    internal enum AudioKeysStatus
    {
        None = 0,
        Partial = 1,
        Installed = 2
    }
}

#endif
