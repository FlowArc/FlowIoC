using System;

namespace FlowIoC.AssetModule.Data
{
    /// <summary>
    /// What a caller may ask of a group load or a download beyond the load itself. Progress is
    /// 0..1, reported once per frame while anything is in flight and 1f once at the end -
    /// what a loading step's Progress is fed from. Background lowers
    /// Application.backgroundLoadingPriority for the life of the load, which is a global setting:
    /// a foreground load that overlaps runs at the lower priority too, so a game that cares waits
    /// for its boot to end before it preloads silently.
    /// </summary>
    public struct AssetLoadOptions
    {
        public IProgress<float> Progress;
        public bool Background;
    }
}
