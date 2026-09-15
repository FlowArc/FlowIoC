#if UNITY_EDITOR

using UnityEngine;

namespace Modules.MobileNotificationModule.Data.ValueObjects
{
    /// <summary>
    /// What the phone would draw for one notification, gathered for the panel's preview: the
    /// texts already formatted, the icons resolved to textures (null where the OS would fall back
    /// to the app icon), the picture when the template names one, the app's name and the time
    /// label the tray shows.
    /// </summary>
    public class NotificationCardEVO
    {
        public string AppName;
        public string Title;
        public string Body;
        public string Time;
        public string Channel;
        public Texture2D SmallIcon;
        public Texture2D LargeIcon;
        public Texture2D AppIcon;
        public Texture2D Picture;
        public Color Accent;
    }
}

#endif
