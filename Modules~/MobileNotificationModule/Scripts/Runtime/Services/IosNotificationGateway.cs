#if UNITY_IOS

using System;
using System.Collections;
using System.Collections.Generic;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.Provider.Coroutine;
using Modules.MobileNotificationModule.Data.ValueObjects;
using Modules.MobileNotificationModule.Enums;
using Unity.Notifications.iOS;
using UnityEngine;

namespace Modules.MobileNotificationModule.Services
{
    /// <summary>
    /// The iOS side of Unity's Mobile Notifications package. There are no channels to register;
    /// the channel key becomes the thread the notifications group under. The identifier is the
    /// identity's string, so the same identity replaces, and it is written into Data so the app
    /// can read back which one opened it. Written on Windows against the package source; the
    /// first Xcode build is its test.
    /// </summary>
    internal class IosNotificationGateway : INotificationGateway
    {
        [Inject] private ICoroutineProvider _coroutines { get; set; }

        public void Initialize(IReadOnlyList<NotificationChannelCVO> channels)
        {
        }

        public NotificationPermission ReadPermission()
        {
            switch (iOSNotificationCenter.GetNotificationSettings().AuthorizationStatus)
            {
                case AuthorizationStatus.Authorized:
                case AuthorizationStatus.Provisional:
                case AuthorizationStatus.Ephemeral:
                    return NotificationPermission.Granted;
                case AuthorizationStatus.Denied:
                    return NotificationPermission.Denied;
                default:
                    return NotificationPermission.NotAsked;
            }
        }

        public void RequestPermission(Action<NotificationPermission> answered) =>
            _coroutines.StartCoroutine(Request(answered));

        private IEnumerator Request(Action<NotificationPermission> answered)
        {
            const AuthorizationOption options = AuthorizationOption.Alert | AuthorizationOption.Badge | AuthorizationOption.Sound;

            using (var request = new AuthorizationRequest(options, false))
            {
                while (!request.IsFinished)
                    yield return null;

                answered(request.Granted ? NotificationPermission.Granted : NotificationPermission.Denied);
            }
        }

        public void Schedule(NotificationDraftVO draft)
        {
            var notification = new iOSNotification
            {
                Identifier = draft.Identity.Identifier,
                Title = draft.Title,
                Body = draft.Body,
                Data = draft.Identity.Identifier,
                ShowInForeground = draft.Template.ShowInForeground,
                ForegroundPresentationOption = PresentationOption.Alert | PresentationOption.Sound | PresentationOption.Badge,
                ThreadIdentifier = draft.Channel.Key,
                Trigger = Trigger(draft)
            };

            // The system copies the attachment into its own store, so the file may stay in the app bundle.
            if (!string.IsNullOrEmpty(draft.PicturePath))
            {
                notification.Attachments = new List<iOSNotificationAttachment>
                {
                    new() {Id = "picture", Url = new Uri(draft.PicturePath).AbsoluteUri}
                };
            }

            iOSNotificationCenter.ScheduleNotification(notification);
        }

        // A delay is an interval trigger, repeating at that interval when asked. A time is a
        // calendar trigger; a repeating one names only the time of day, which is what makes it daily.
        private iOSNotificationTrigger Trigger(NotificationDraftVO draft)
        {
            if (draft.Interval.HasValue)
            {
                TimeSpan interval = draft.FireTime - DateTime.Now;

                if (interval < TimeSpan.FromSeconds(1))
                    interval = TimeSpan.FromSeconds(1);

                return new iOSNotificationTimeIntervalTrigger
                {
                    TimeInterval = draft.Repeats ? draft.RepeatInterval : interval,
                    Repeats = draft.Repeats
                };
            }

            var calendar = new iOSNotificationCalendarTrigger
            {
                Hour = draft.FireTime.Hour,
                Minute = draft.FireTime.Minute,
                Second = draft.FireTime.Second,
                Repeats = draft.Repeats
            };

            if (!draft.Repeats)
            {
                calendar.Year = draft.FireTime.Year;
                calendar.Month = draft.FireTime.Month;
                calendar.Day = draft.FireTime.Day;
            }

            return calendar;
        }

        public void Cancel(NotificationIdentityVO identity)
        {
            iOSNotificationCenter.RemoveScheduledNotification(identity.Identifier);
            iOSNotificationCenter.RemoveDeliveredNotification(identity.Identifier);
        }

        public void CancelAllScheduled() => iOSNotificationCenter.RemoveAllScheduledNotifications();

        public void ClearDelivered()
        {
            iOSNotificationCenter.RemoveAllDeliveredNotifications();
            iOSNotificationCenter.ApplicationBadge = 0;
        }

        public string ReadOpenedFrom() => iOSNotificationCenter.GetLastRespondedNotification()?.Data;

        public void OpenSettings() => iOSNotificationCenter.OpenNotificationSettings();

        // StreamingAssets is a plain folder inside the iOS app bundle, so a picture is read from where it ships.
        public void PreparePictures(IReadOnlyList<string> pictures)
        {
        }

        public bool TryResolvePicture(string picture, out string path)
        {
            path = System.IO.Path.Combine(Application.streamingAssetsPath, picture ?? string.Empty);

            if (!string.IsNullOrEmpty(picture) && System.IO.File.Exists(path))
                return true;

            path = null;
            return false;
        }
    }
}

#endif
