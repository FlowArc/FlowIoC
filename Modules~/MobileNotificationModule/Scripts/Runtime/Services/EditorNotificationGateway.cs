using System;
using System.Collections;
using System.Collections.Generic;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.Provider.Coroutine;
using FlowIoC.ConsoleModule;
using Modules.MobileNotificationModule.Data.ValueObjects;
using Modules.MobileNotificationModule.Enums;
using UnityEngine;

namespace Modules.MobileNotificationModule.Services
{
    /// <summary>
    /// The Editor never shows a notification. This gateway keeps what was scheduled, logs every
    /// call on the module's channel so a flow reads in the Flow Console, and once a second while
    /// playing delivers what is due - as a log line - so the test scene shows a notification
    /// arriving without a device. Permission is always granted here.
    /// </summary>
    internal class EditorNotificationGateway : INotificationGateway
    {
        [Inject] private ICoroutineProvider _coroutines { get; set; }

        private readonly List<NotificationDraftVO> _scheduled = new();

        public IReadOnlyList<NotificationDraftVO> Scheduled => _scheduled;

        public void Initialize(IReadOnlyList<NotificationChannelCVO> channels)
        {
            FlowLogger.Log($"Editor gateway - Initialize, {channels.Count} channel(s) declared, nothing to register");
            _coroutines?.StartCoroutine(Watch());
        }

        public NotificationPermission ReadPermission() => NotificationPermission.Granted;

        public void RequestPermission(Action<NotificationPermission> answered)
        {
            FlowLogger.Log("Editor gateway - RequestPermission, granted");
            answered(NotificationPermission.Granted);
        }

        public void Schedule(NotificationDraftVO draft)
        {
            _scheduled.RemoveAll(scheduled => scheduled.Identity.Identifier == draft.Identity.Identifier);
            _scheduled.Add(draft);
            FlowLogger.Log($"Editor gateway - Schedule {draft.Identity.Identifier} at {draft.FireTime:HH:mm:ss}"
                           + (draft.Repeats ? $", every {draft.RepeatInterval}" : string.Empty));
        }

        public void Cancel(NotificationIdentityVO identity)
        {
            _scheduled.RemoveAll(scheduled => scheduled.Identity.Identifier == identity.Identifier);
            FlowLogger.Log($"Editor gateway - Cancel {identity.Identifier}");
        }

        public void CancelAllScheduled()
        {
            _scheduled.Clear();
            FlowLogger.Log("Editor gateway - CancelAllScheduled");
        }

        public void ClearDelivered() => FlowLogger.Log("Editor gateway - ClearDelivered");

        public string ReadOpenedFrom() => null;

        public void OpenSettings() => FlowLogger.Log("Editor gateway - OpenSettings, nothing opens in the Editor");

        // In the Editor StreamingAssets is a plain folder under Assets, so a picture is where it was authored.
        public void PreparePictures(IReadOnlyList<string> pictures) =>
            FlowLogger.Log($"Editor gateway - PreparePictures, {pictures.Count} picture(s) read from Assets/StreamingAssets");

        public bool TryResolvePicture(string picture, out string path)
        {
            path = System.IO.Path.Combine(Application.streamingAssetsPath, picture ?? string.Empty);

            if (!string.IsNullOrEmpty(picture) && System.IO.File.Exists(path))
                return true;

            path = null;
            return false;
        }

        /// <summary>Delivers what is due: logs it, then re-arms a repeating draft or drops it.</summary>
        public void Tick(DateTime now)
        {
            for (int i = _scheduled.Count - 1; i >= 0; i--)
            {
                NotificationDraftVO draft = _scheduled[i];

                if (draft.FireTime > now)
                    continue;

                FlowLogger.Log($"Editor gateway - Delivered {draft.Identity.Identifier}: {draft.Title} - {draft.Body}"
                               + (draft.PicturePath == null ? string.Empty : $", picture {draft.Template?.Picture}"));

                if (draft.Repeats)
                    draft.FireTime = draft.FireTime.Add(draft.RepeatInterval);
                else
                    _scheduled.RemoveAt(i);
            }
        }

        private IEnumerator Watch()
        {
            var second = new WaitForSecondsRealtime(1f);

            while (true)
            {
                yield return second;
                Tick(DateTime.Now);
            }
        }
    }
}
