namespace Modules.AdsModule.Enums
{
    /// <summary>What the module knows about the ad of one format.</summary>
    public enum AdLoadState
    {
        Idle = 0,
        Loading = 1,
        Ready = 2,
        WaitingRetry = 3
    }
}
