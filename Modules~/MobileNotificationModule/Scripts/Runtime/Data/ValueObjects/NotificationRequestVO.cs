using System;

namespace Modules.MobileNotificationModule.Data.ValueObjects
{
    /// <summary>
    /// What the service hands its command: which template, for which instance, when. At most
    /// one of After and At is set for a schedule (neither means the template's default time); a
    /// cancel carries Key and Tag only.
    /// </summary>
    public class NotificationRequestVO
    {
        public string Key;
        public string Tag;
        public TimeSpan? After;
        public DateTime? At;
        public object[] Args;
    }
}
