namespace Modules.AssetDeliveryModule.Data.ValueObjects
{
    /// <summary>What the platform says about one pack right now.</summary>
    public class AssetPackStateRVO
    {
        public string Pack;
        public bool OnDevice;
        public long TotalBytes;
        public long DownloadedBytes;
    }
}
