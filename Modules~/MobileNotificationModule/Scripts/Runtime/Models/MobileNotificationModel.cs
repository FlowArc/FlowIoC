using System.Collections.Generic;
using FlowIoC.BaseModule.Adapters;
using FlowIoC.BaseModule.Constructables;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.MobileNotificationModule.Data.UnityObjects;
using Modules.MobileNotificationModule.Data.ValueObjects;
using Modules.MobileNotificationModule.Enums;
using Modules.MobileNotificationModule.RootsContexts;
using UnityEngine;

namespace Modules.MobileNotificationModule.Models
{
    /// <summary>
    /// PostConstruct only takes the catalogue off the Root's adapter and indexes it, dropping each
    /// entry that cannot be used and saying why at the asset. Everything else is a plain Set or a
    /// plain read.
    /// </summary>
    public class MobileNotificationModel : IMobileNotificationModel, IConstructable
    {
        [Inject(nameof(MobileNotificationServiceContext))]
        private GameObject _root { get; set; }

        private readonly List<NotificationChannelCVO> _channels = new();
        private readonly List<ReturnReminderCVO> _returnReminders = new();
        private readonly List<string> _pictures = new();
        private readonly Dictionary<string, NotificationChannelCVO> _channelsByKey = new();
        private readonly Dictionary<string, NotificationCVO> _notificationsByKey = new();
        private readonly List<NotificationDraftVO> _scheduled = new();

        public bool IsPostConstructed { get; set; }
        public bool IsDeconstructed { get; set; }

        public IReadOnlyList<NotificationChannelCVO> Channels => _channels;

        public IReadOnlyList<ReturnReminderCVO> ReturnReminders => _returnReminders;

        public IReadOnlyList<string> Pictures => _pictures;

        public NotificationPermission Permission { get; private set; } = NotificationPermission.NotAsked;

        public string OpenedFromKey { get; private set; } = string.Empty;

        public string OpenedFromTag { get; private set; } = string.Empty;

        public IReadOnlyList<NotificationDraftVO> Scheduled => _scheduled;

        public void PostConstruct()
        {
            _channels.Clear();
            _channelsByKey.Clear();
            _notificationsByKey.Clear();
            _returnReminders.Clear();
            _pictures.Clear();

            if (TryReadAdapter(out CD_MobileNotifications catalogue))
                Read(catalogue);
        }

        public bool TryGetNotification(string key, out NotificationCVO notification) =>
            _notificationsByKey.TryGetValue(key ?? string.Empty, out notification);

        public bool TryGetChannel(string key, out NotificationChannelCVO channel) =>
            _channelsByKey.TryGetValue(key ?? string.Empty, out channel);

        public void SetPermission(NotificationPermission permission) => Permission = permission;

        public void SetOpenedFrom(NotificationIdentityVO identity)
        {
            OpenedFromKey = identity?.Key ?? string.Empty;
            OpenedFromTag = identity?.Tag ?? string.Empty;
        }

        public void MarkScheduled(NotificationDraftVO draft)
        {
            MarkCancelled(draft.Identity.Identifier);
            _scheduled.Add(draft);
        }

        public void MarkCancelled(string identifier) =>
            _scheduled.RemoveAll(draft => draft.Identity.Identifier == identifier);

        public void ClearScheduled() => _scheduled.Clear();

        private void Read(CD_MobileNotifications catalogue)
        {
            foreach (NotificationChannelCVO channel in catalogue.Channels)
            {
                if (string.IsNullOrEmpty(channel.Key))
                {
                    FlowLogger.LogError("CD_MobileNotifications has a channel with no key; it is ignored.", catalogue);
                    continue;
                }

                if (_channelsByKey.ContainsKey(channel.Key))
                {
                    FlowLogger.LogError($"CD_MobileNotifications declares channel '{channel.Key}' twice; the second is ignored.", catalogue);
                    continue;
                }

                _channelsByKey.Add(channel.Key, channel);
                _channels.Add(channel);
            }

            foreach (NotificationCVO notification in catalogue.Notifications)
            {
                if (string.IsNullOrEmpty(notification.Key))
                {
                    FlowLogger.LogError("CD_MobileNotifications has a notification with no key; it is ignored.", catalogue);
                    continue;
                }

                if (_notificationsByKey.ContainsKey(notification.Key))
                {
                    FlowLogger.LogError($"CD_MobileNotifications declares notification '{notification.Key}' twice; the second is ignored.", catalogue);
                    continue;
                }

                if (!_channelsByKey.ContainsKey(notification.Channel ?? string.Empty))
                {
                    FlowLogger.LogError($"Notification '{notification.Key}' names channel '{notification.Channel}', which CD_MobileNotifications does not declare; it is ignored.", catalogue);
                    continue;
                }

                _notificationsByKey.Add(notification.Key, notification);

                if (!string.IsNullOrEmpty(notification.Picture) && !_pictures.Contains(notification.Picture))
                    _pictures.Add(notification.Picture);
            }

            foreach (ReturnReminderCVO reminder in catalogue.ReturnReminders)
            {
                if (!_notificationsByKey.ContainsKey(reminder.Notification ?? string.Empty))
                {
                    FlowLogger.LogError($"Return reminder names notification '{reminder.Notification}', which CD_MobileNotifications does not declare; it is ignored.", catalogue);
                    continue;
                }

                if (reminder.AfterMinutes <= 0f)
                {
                    FlowLogger.LogError($"Return reminder '{reminder.Notification}' has no AfterMinutes; it is ignored.", catalogue);
                    continue;
                }

                _returnReminders.Add(reminder);
            }
        }

        /// <summary>
        /// The adapter on the Root is where the catalogue is filed, under its type name. A missing
        /// asset is reported by the adapter itself, naming the asset and the Root.
        /// </summary>
        private bool TryReadAdapter(out CD_MobileNotifications catalogue)
        {
            catalogue = null;

            RootAdapter adapter = _root != null ? _root.GetComponent<RootAdapter>() : null;

            if (adapter == null)
            {
                FlowLogger.LogError("MobileNotificationServiceRoot has no RootAdapter, so no notification can be scheduled.", _root);
                return false;
            }

            catalogue = adapter.GetScriptable<CD_MobileNotifications>();
            return catalogue != null;
        }
    }
}
