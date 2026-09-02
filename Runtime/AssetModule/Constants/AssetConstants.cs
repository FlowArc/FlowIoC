namespace FlowIoC.AssetModule.Constants
{
    public static class AssetConstants
    {
        // Bootstrapped AssetService must initialize before any consumer context. Initialize Order
        // runs from -100 to 100, Services take the negative band, and the asset service is the
        // last of them - one step ahead of the game's own modules, which start at 0.
        public const int InitializeOrder = -1;
    }
}
