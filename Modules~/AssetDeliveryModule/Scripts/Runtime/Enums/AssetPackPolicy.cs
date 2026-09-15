namespace Modules.AssetDeliveryModule.Enums
{
    /// <summary>
    /// How a pack reaches the device. The Addressables group's Play Asset Delivery schema is the
    /// one place it is set; iOS reads the same value as essential, prefetch and onDemand.
    /// </summary>
    public enum AssetPackPolicy
    {
        InstallTime = 0,
        FastFollow = 1,
        OnDemand = 2
    }
}
