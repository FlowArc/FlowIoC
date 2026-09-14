namespace FlowIoC.AssetModule.Constants
{
    public static class AssetConstants
    {
        // Where AssetServiceRoot sits. The services sit on the tens of the negative band in the
        // order the boot reads: -100 and -90 are the seats of the modules that put data in place,
        // then the asset service at -80 - the door every Addressables load goes through - and the
        // screen and pool services that load through it at -70 and -60. Nothing binds against it:
        // the screen and pool loaders resolve the service at their first load, so the number is
        // reading order, not a dependency.
        public const int InitializeOrder = -80;
    }
}
