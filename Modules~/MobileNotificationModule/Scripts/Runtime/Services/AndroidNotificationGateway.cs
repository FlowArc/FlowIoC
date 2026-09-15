#if UNITY_ANDROID

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.Provider.Coroutine;
using FlowIoC.ConsoleModule;
using Modules.MobileNotificationModule.Data.ValueObjects;
using Modules.MobileNotificationModule.Enums;
using Unity.Notifications.Android;
using UnityEngine;
using UnityEngine.Networking;

namespace Modules.MobileNotificationModule.Services
{
    /// <summary>
    /// The Android side of Unity's Mobile Notifications package. Channels are registered once
    /// with the importance the catalogue gave them; a notification is sent with the identity's
    /// int id, so the same identity replaces, and its identifier in IntentData, so the app can
    /// read back which one opened it. Inexact scheduling: exact alarms are not requested.
    ///
    /// A picture is a file the Android side decodes from a path, and StreamingAssets on Android is
    /// inside the APK, not a path - so each picture the catalogue names is copied once into
    /// persistentDataPath at initialize, and a notification carries that copy as its big picture.
    /// </summary>
    internal class AndroidNotificationGateway : INotificationGateway
    {
        private const string PICTURES_FOLDER = "flowioc-notifications";

        [Inject] private ICoroutineProvider _coroutines { get; set; }

        public void Initialize(IReadOnlyList<NotificationChannelCVO> channels)
        {
            AndroidNotificationCenter.Initialize();

            foreach (NotificationChannelCVO channel in channels)
            {
                AndroidNotificationCenter.RegisterNotificationChannel(new AndroidNotificationChannel
                {
                    Id = channel.Key,
                    Name = channel.Name,
                    Description = channel.Description,
                    Importance = ToImportance(channel.Importance),
                    EnableVibration = true,
                    CanShowBadge = true
                });
            }
        }

        public NotificationPermission ReadPermission()
        {
            switch (AndroidNotificationCenter.UserPermissionToPost)
            {
                case PermissionStatus.Allowed:
                    return NotificationPermission.Granted;
                case PermissionStatus.Denied:
                case PermissionStatus.DeniedDontAskAgain:
                case PermissionStatus.NotificationsBlockedForApp:
                    return NotificationPermission.Denied;
                default:
                    return NotificationPermission.NotAsked;
            }
        }

        public void RequestPermission(Action<NotificationPermission> answered) =>
            _coroutines.StartCoroutine(Request(answered));

        // Below Android 13 the request is answered Allowed at once; above, the dialog decides.
        private IEnumerator Request(Action<NotificationPermission> answered)
        {
            var request = new PermissionRequest();

            while (request.Status == PermissionStatus.RequestPending)
                yield return null;

            answered(request.Status == PermissionStatus.Allowed
                ? NotificationPermission.Granted
                : NotificationPermission.Denied);
        }

        public void Schedule(NotificationDraftVO draft)
        {
            var notification = new AndroidNotification
            {
                Title = draft.Title,
                Text = draft.Body,
                FireTime = draft.FireTime,
                ShowInForeground = draft.Template.ShowInForeground,
                IntentData = draft.Identity.Identifier,
                Group = draft.Channel.Key,
                Style = NotificationStyle.BigTextStyle
            };

            if (!string.IsNullOrEmpty(draft.Template.SmallIcon))
                notification.SmallIcon = draft.Template.SmallIcon;

            if (!string.IsNullOrEmpty(draft.Template.LargeIcon))
                notification.LargeIcon = draft.Template.LargeIcon;

            if (draft.Repeats)
                notification.RepeatInterval = draft.RepeatInterval;

            // The picture shows under the text once the notification is expanded; collapsed, the
            // card keeps the large icon, so ShowWhenCollapsed stays off. Expanded, Android drops
            // the body and shows the summary text in its place, so the body goes there too - a
            // phone (CPH2747, Android 16) showed the title and the picture with no body without it.
            if (!string.IsNullOrEmpty(draft.PicturePath))
            {
                notification.Style = NotificationStyle.BigPictureStyle;
                notification.BigPicture = new BigPictureStyle
                {
                    Picture = draft.PicturePath,
                    LargeIcon = notification.LargeIcon,
                    ContentTitle = draft.Title,
                    SummaryText = draft.Body,
                    ContentDescription = draft.Body,
                    ShowWhenCollapsed = false
                };
            }

            AndroidNotificationCenter.SendNotificationWithExplicitID(notification, draft.Channel.Key, draft.Identity.Id);
        }

        public void Cancel(NotificationIdentityVO identity) => AndroidNotificationCenter.CancelNotification(identity.Id);

        public void CancelAllScheduled() => AndroidNotificationCenter.CancelAllScheduledNotifications();

        public void ClearDelivered() => AndroidNotificationCenter.CancelAllDisplayedNotifications();

        public string ReadOpenedFrom() => AndroidNotificationCenter.GetLastNotificationIntent()?.Notification.IntentData;

        public void OpenSettings() => AndroidNotificationCenter.OpenNotificationSettings();

        public void PreparePictures(IReadOnlyList<string> pictures)
        {
            if (pictures.Count > 0)
                _coroutines.StartCoroutine(CopyPictures(pictures));
        }

        public bool TryResolvePicture(string picture, out string path)
        {
            path = CopyPath(picture);

            if (!string.IsNullOrEmpty(picture) && File.Exists(path))
                return true;

            path = null;
            return false;
        }

        // StreamingAssets on Android is read through a web request; the copy lands where the
        // notification service can decode a file. A copy already there is kept.
        private IEnumerator CopyPictures(IReadOnlyList<string> pictures)
        {
            foreach (string picture in pictures)
            {
                string target = CopyPath(picture);

                if (File.Exists(target))
                    continue;

                string source = Path.Combine(Application.streamingAssetsPath, picture);

                using (UnityWebRequest request = UnityWebRequest.Get(source))
                {
                    yield return request.SendWebRequest();

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        FlowLogger.LogError(
                            $"Picture '{picture}' is not under StreamingAssets, so notifications naming it go out without it: {request.error}");
                        continue;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    File.WriteAllBytes(target, request.downloadHandler.data);
                    FlowLogger.Log($"Picture '{picture}' copied for the notification service");
                }
            }
        }

        private string CopyPath(string picture) =>
            Path.Combine(Application.persistentDataPath, PICTURES_FOLDER, picture ?? string.Empty);

        private Importance ToImportance(NotificationImportance importance)
        {
            switch (importance)
            {
                case NotificationImportance.Low: return Importance.Low;
                case NotificationImportance.High: return Importance.High;
                default: return Importance.Default;
            }
        }
    }
}

#endif