using Modules.AssetDeliveryModule.Enums;

namespace Modules.AssetDeliveryModule.Data.ValueObjects
{
    /// <summary>The manifest joined with the platform's state - what the service hands a caller.</summary>
    public class AssetPackRVO
    {
        public string Pack;
        public string Group;
        public AssetPackPolicy Policy;
        public bool OnDevice;
        public long TotalBytes;
    }
}
