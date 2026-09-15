using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.MobileNotificationModule.Data.ValueObjects;
using Modules.MobileNotificationModule.Enums;
using Modules.MobileNotificationModule.Models;
using Modules.MobileNotificationModule.Services;

namespace Modules.MobileNotificationModule.Controllers
{
    /// <summary>
    /// Turns a request into a draft - template, channel, time, formatted texts, identity - and
    /// hands it to the platform. Every refusal names what the developer typed. A denied
    /// permission is the player's choice and is a plain log; NotAsked still schedules, because
    /// Android below 13 needs no permission and iOS simply does not deliver.
    /// </summary>
    internal class ScheduleNotificationCommand : Command
    {
        private const int MIN_REPEAT_SECONDS = 60;

        [Inject] private IMobileNotificationModel _model { get; set; }
        [Inject] private INotificationGateway _gateway { get; set; }

        [SignalParam] private NotificationRequestVO _request { get; set; }

        public override void Execute()
        {
            if (!TryBuildDraft(_request, out NotificationDraftVO draft))
                return;

            if (_model.Permission == NotificationPermission.Denied)
            {
                FlowLogger.Log($"Schedule - {draft.Identity.Identifier} not scheduled, permission denied");
                return;
            }

            try
            {
                _gateway.Schedule(draft);
            }
            catch (Exception e)
            {
                FlowLogger.LogError($"Schedule - {draft.Identity.Identifier} refused by the platform: " + e.Message);
                return;
            }

            _model.MarkScheduled(draft);
            FlowLogger.Log($"Schedule - {draft.Identity.Identifier} at {draft.FireTime:yyyy-MM-dd HH:mm:ss}"
                           + (draft.Repeats ? $", every {draft.RepeatInterval}" : string.Empty));
        }

        private bool TryBuildDraft(NotificationRequestVO request, out NotificationDraftVO draft)
        {
            draft = null;

            if (!_model.TryGetNotification(request.Key, out NotificationCVO template))
            {
                FlowLogger.LogError($"Schedule - no notification '{request.Key}' in CD_MobileNotifications.");
                return false;
            }

            if (!_model.TryGetChannel(template.Channel, out NotificationChannelCVO channel))
            {
                FlowLogger.LogError($"Schedule - notification '{request.Key}' names channel '{template.Channel}', which CD_MobileNotifications does not declare.");
                return false;
            }

            DateTime now = DateTime.Now;
            DateTime fireTime;
            TimeSpan? interval;

            if (request.At.HasValue)
            {
                fireTime = request.At.Value;
                interval = null;
            }
            else if (request.After.HasValue)
            {
                fireTime = now.Add(request.After.Value);
                interval = request.After.Value;
            }
            else if (template.DefaultAfterMinutes > 0f)
            {
                interval = TimeSpan.FromMinutes(template.DefaultAfterMinutes);
                fireTime = now.Add(interval.Value);
            }
            else
            {
                FlowLogger.LogError($"Schedule - notification '{request.Key}' was given no time and has no DefaultAfterMinutes.");
                return false;
            }

            TimeSpan repeatInterval = template.Repeats
                ? interval ?? TimeSpan.FromDays(1)
                : TimeSpan.Zero;

            if (template.Repeats && repeatInterval.TotalSeconds < MIN_REPEAT_SECONDS)
            {
                FlowLogger.LogError($"Schedule - notification '{request.Key}' repeats every {repeatInterval}, under 60 s, which iOS refuses.");
                return false;
            }

            if (fireTime <= now)
            {
                // A daily reminder whose time passed today is tomorrow's; anything else is now.
                if (template.Repeats && !interval.HasValue)
                    while (fireTime <= now) fireTime = fireTime.AddDays(1);
                else
                    fireTime = now.AddSeconds(1);
            }

            string title;
            string body;

            try
            {
                title = Format(template.Title, request.Args);
                body = Format(template.Body, request.Args);
            }
            catch (FormatException)
            {
                int given = request.Args?.Length ?? 0;
                FlowLogger.LogError($"Schedule - notification '{request.Key}' has a placeholder its {given} argument(s) do not fill.");
                return false;
            }

            draft = new NotificationDraftVO
            {
                Identity = new NotificationIdentityVO(request.Key, request.Tag),
                Template = template,
                Channel = channel,
                Title = title,
                Body = body,
                FireTime = fireTime,
                Interval = interval,
                Repeats = template.Repeats,
                RepeatInterval = repeatInterval,
                PicturePath = ResolvePicture(request.Key, template.Picture)
            };

            return true;
        }

        // A picture not on the device yet - the copy still in flight, or the file never shipped -
        // is not a reason to lose the notification; it goes out without it, and the log says so.
        private string ResolvePicture(string key, string picture)
        {
            if (string.IsNullOrEmpty(picture))
                return null;

            if (_gateway.TryResolvePicture(picture, out string path))
                return path;

            FlowLogger.Log($"Schedule - picture '{picture}' of '{key}' is not on the device, so it goes out without it");
            return null;
        }

        private string Format(string text, object[] args) =>
            string.IsNullOrEmpty(text) ? string.Empty : string.Format(text, args ?? Array.Empty<object>());
    }
}
