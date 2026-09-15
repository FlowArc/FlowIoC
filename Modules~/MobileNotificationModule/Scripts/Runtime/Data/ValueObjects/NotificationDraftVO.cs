using System;

namespace Modules.MobileNotificationModule.Data.ValueObjects
{
    /// <summary>
    /// What a gateway schedules, and what the model keeps as its mirror of what was scheduled.
    /// Title and Body are already formatted. FireTime is local time. Interval is the After the
    /// caller gave (or the template's default), null when the caller gave At. RepeatInterval is
    /// zero unless Repeats. PicturePath is the image on the device, or null when the template
    /// names none or the file is not there yet.
    /// </summary>
    public class NotificationDraftVO
    {
        public NotificationIdentityVO Identity;
        public NotificationCVO Template;
        public NotificationChannelCVO Channel;
        public string Title;
        public string Body;
        public DateTime FireTime;
        public TimeSpan? Interval;
        public bool Repeats;
        public TimeSpan RepeatInterval;
        public string PicturePath;
    }
}
