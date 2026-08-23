#if UNITY_EDITOR

namespace FlowIoC.Editor.Migration
{
    /// <summary>
    /// One asset's old and new location, both as Unity asset paths.
    /// </summary>
    internal class LegacyAssetMove
    {
        public LegacyAssetMove(string legacy, string destination)
        {
            Legacy = legacy;
            Destination = destination;
        }

        public string Legacy { get; }
        public string Destination { get; }
    }
}

#endif
