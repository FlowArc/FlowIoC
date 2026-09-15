using System;
using Modules.MobileNotificationModule.Enums;

namespace Modules.MobileNotificationModule.Data.ValueObjects
{
    /// <summary>
    /// A category the player sees in the OS notification settings and can mute on its own,
    /// registered once at initialize. Its Key is what a notification names as its Channel.
    /// </summary>
    [Serializable]
    public class NotificationChannelCVO
    {
        public string Key;
        public string Name;
        public string Description;
        public NotificationImportance Importance = NotificationImportance.Default;
    }
}
