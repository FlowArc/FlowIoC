namespace Modules.AssetDeliveryModule.Constants
{
    public static class AssetDeliveryConstants
    {
        /// <summary>The loading step EnsurePromised reports. A game lists it in the set its boot runs.</summary>
        public const string CONTENT_STEP = "Content";

        /// <summary>Written beside the catalog at build time; read from Addressables.RuntimePath.</summary>
        public const string MANIFEST_FILE = "FlowAssetPacks.json";
    }
}
