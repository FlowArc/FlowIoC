using System.Collections.Generic;
using Modules.MobileNotificationModule.Data.ValueObjects;
using UnityEngine;

namespace Modules.MobileNotificationModule.Data.UnityObjects
{
    /// <summary>
    /// The catalogue: the channels the player can mute, the notifications the game schedules by
    /// key, and the reminders that go out by themselves when the app is left. Only this module
    /// reads it, which is why it stays in the Runtime assembly rather than in Shared. It is filed
    /// in the module's own Scriptables slot on MobileNotificationServiceRoot's adapter.
    /// </summary>
    [CreateAssetMenu(fileName = "CD_MobileNotifications", menuName = "FlowIoC/MobileNotificationModule/Data/CD_MobileNotifications")]
    public class CD_MobileNotifications : ScriptableObject
    {
        [SerializeField] private List<NotificationChannelCVO> _channels = new();
        [SerializeField] private List<NotificationCVO> _notifications = new();
        [SerializeField] private List<ReturnReminderCVO> _returnReminders = new();

        public List<NotificationChannelCVO> Channels => _channels;

        public List<NotificationCVO> Notifications => _notifications;

        public List<ReturnReminderCVO> ReturnReminders => _returnReminders;
    }
}
