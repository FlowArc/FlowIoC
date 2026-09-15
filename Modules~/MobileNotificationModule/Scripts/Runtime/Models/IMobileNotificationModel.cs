using System.Collections.Generic;
using Modules.MobileNotificationModule.Data.ValueObjects;
using Modules.MobileNotificationModule.Enums;

namespace Modules.MobileNotificationModule.Models
{
    /// <summary>
    /// The module's state: the catalogue as read off the Root's adapter, the permission the OS
    /// last reported, the notification that opened the app, and a mirror of what this session
    /// scheduled. It decides nothing; the commands do.
    /// </summary>
    public interface IMobileNotificationModel
    {
        IReadOnlyList<NotificationChannelCVO> Channels { get; }
        IReadOnlyList<ReturnReminderCVO> ReturnReminders { get; }
        /// <summary>Every picture the templates name, once each, for the gateway to ready on the device.</summary>
        IReadOnlyList<string> Pictures { get; }
        bool TryGetNotification(string key, out NotificationCVO notification);
        bool TryGetChannel(string key, out NotificationChannelCVO channel);

        NotificationPermission Permission { get; }
        void SetPermission(NotificationPermission permission);

        string OpenedFromKey { get; }
        string OpenedFromTag { get; }
        /// <summary>Null clears both.</summary>
        void SetOpenedFrom(NotificationIdentityVO identity);

        IReadOnlyList<NotificationDraftVO> Scheduled { get; }
        /// <summary>Replaces a draft with the same identifier.</summary>
        void MarkScheduled(NotificationDraftVO draft);
        void MarkCancelled(string identifier);
        void ClearScheduled();
    }
}
