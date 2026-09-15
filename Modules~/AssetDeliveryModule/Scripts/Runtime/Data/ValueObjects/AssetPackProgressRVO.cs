namespace Modules.AssetDeliveryModule.Data.ValueObjects
{
    /// <summary>Bytes across every pack of one download.</summary>
    public class AssetPackProgressRVO
    {
        public long DownloadedBytes;
        public long TotalBytes;

        public float Fraction => TotalBytes <= 0 ? 1f : (float) DownloadedBytes / TotalBytes;
    }
}
