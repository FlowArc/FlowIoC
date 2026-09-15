using System;
using UnityEngine;

namespace Modules.MobileNotificationModule.Data.ValueObjects
{
    /// <summary>
    /// A notification template, scheduled by its Key. Title and Body may carry {0}, {1}
    /// placeholders that the caller's args fill. SmallIcon and LargeIcon are Android icon names
    /// registered under Project Settings > Mobile Notifications; iOS ignores them. Picture is an
    /// image file under Assets/StreamingAssets, as "Notifications/chest.png": Android shows it
    /// under the text when the notification is expanded, iOS as the banner's thumbnail and the
    /// full image on a long press. Repeats means "at the interval it was scheduled with";
    /// DefaultAfterMinutes is the time a bound Commands.Schedule step uses, 0 meaning the caller
    /// always gives one.
    /// </summary>
    [Serializable]
    public class NotificationCVO
    {
        public string Key;
        public string Channel;
        public string Title;
        [TextArea] public string Body;
        public string SmallIcon;
        public string LargeIcon;
        public string Picture;
        public bool ShowInForeground;
        public bool Repeats;
        public float DefaultAfterMinutes;
    }
}