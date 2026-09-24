#if UNITY_EDITOR
namespace Modules.WorldPointerModule.WorldPointerSampleScreenModule.Shared.Constants
{
    /// <summary>
    /// The ids the sample screen draws, one per off-screen mode, because a display's options are
    /// its own and two ids drawn differently are two displays. In a game these sit in the Shared
    /// assembly of the module that owns the targets.
    /// </summary>
    public static class WorldPointerSampleIds
    {
        public const string Hide = "SampleHide";
        public const string Clamp = "SampleClamp";
        public const string Ignore = "SampleIgnore";
    }
}
#endif
