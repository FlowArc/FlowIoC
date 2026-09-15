#if UNITY_EDITOR

using System.Text;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.MobileNotificationModule.Data.ValueObjects;
using Modules.MobileNotificationModule.MobileNotificationTestModule.Signals;
using Modules.MobileNotificationModule.Services;

namespace Modules.MobileNotificationModule.MobileNotificationTestModule.Controllers
{
    /// <summary>What the service knows, as the lines the label shows.</summary>
    public class ReportNotificationStateCommand : Command
    {
        [Inject] private IMobileNotificationService _notifications { get; set; }
        [InjectSignal] private MobileNotificationTestInternalSignals _signals { get; set; }

        public override void Execute()
        {
            var text = new StringBuilder();
            text.Append("Permission: ").Append(_notifications.Permission).Append('\n');
            text.Append("Opened from: ")
                .Append(string.IsNullOrEmpty(_notifications.OpenedFromKey) ? "-" : _notifications.OpenedFromKey)
                .Append(string.IsNullOrEmpty(_notifications.OpenedFromTag) ? string.Empty : "#" + _notifications.OpenedFromTag)
                .Append('\n');
            text.Append("Scheduled: ").Append(_notifications.Scheduled.Count).Append('\n');

            foreach (NotificationDraftVO draft in _notifications.Scheduled)
            {
                text.Append("  ").Append(draft.Identity.Identifier)
                    .Append(" at ").Append(draft.FireTime.ToString("HH:mm:ss"))
                    .Append(draft.Repeats ? " repeats" : string.Empty)
                    .Append('\n');
            }

            _signals.StateChanged.Dispatch(text.ToString());
        }
    }
}

#endif
