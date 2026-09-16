#if UNITY_EDITOR

namespace FlowIoC.Editor.ModuleInstall
{
    /// <summary>What an update does with every file both sides changed. One choice for all of them.</summary>
    internal enum ConflictPolicy
    {
        KeepMine = 0,
        TakeTheirs = 1
    }
}

#endif
