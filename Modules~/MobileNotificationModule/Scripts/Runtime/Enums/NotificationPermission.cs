namespace Modules.MobileNotificationModule.Enums
{
    /// <summary>
    /// What the OS has said about posting notifications. NotAsked also covers a request that is
    /// still pending; Android below 13 needs no permission and reports Granted.
    /// </summary>
    public enum NotificationPermission
    {
        NotAsked = 0,
        Granted = 1,
        Denied = 2
    }
}
