using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.MobileNotificationModule.Data.ValueObjects;
using Modules.MobileNotificationModule.Models;
using Modules.MobileNotificationModule.Signals;

namespace Modules.MobileNotificationModule.Controllers
{
    /// <summary>
    /// Runs when the app returns, and at boot: the reminders are for an absent player. Nothing
    /// else is touched - a chest reminder the game scheduled survives the return.
    /// </summary>
    internal class CancelReturnRemindersCommand : Command
    {
        [Inject] private IMobileNotificationModel _model { get; set; }
        [InjectSignal] private MobileNotificationInternalSignals _signals { get; set; }

        public override void Execute()
        {
            FlowLogger.Log($"Returned - taking back {_model.ReturnReminders.Count} return reminder(s)");

            foreach (ReturnReminderCVO reminder in _model.ReturnReminders)
                _signals.Cancel.Dispatch(new NotificationRequestVO {Key = reminder.Notification, Tag = reminder.Tag});
        }
    }
}
