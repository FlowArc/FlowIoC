namespace Modules.AdsModule.Enums
{
    /// <summary>Where the one plugged provider is. None until a plug lands.</summary>
    public enum AdsProviderState
    {
        None = 0,
        Plugged = 1,
        Initializing = 2,
        Ready = 3,
        Failed = 4
    }
}
