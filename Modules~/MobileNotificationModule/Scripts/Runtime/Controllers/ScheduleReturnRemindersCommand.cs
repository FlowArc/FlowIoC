using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.MobileNotificationModule.Data.ValueObjects;
using Modules.MobileNotificationModule.Models;
using Modules.MobileNotificationModule.Signals;

namespace Modules.MobileNotificationModule.Controllers
{
    /// <summary>
    /// Runs when the app is left: one schedule per reminder in the catalogue, through the same
    /// Schedule flow a game call takes, so the permission rule and the diagnostics are one.
    /// </summary>
    internal class ScheduleReturnRemindersCommand : Command
    {
        [Inject] private IMobileNotificationModel _model { get; set; }
        [InjectSignal] private MobileNotificationInternalSignals _signals { get; set; }

        public override void Execute()
        {
            FlowLogger.Log($"Left - {_model.ReturnReminders.Count} return reminder(s)");

            foreach (ReturnReminderCVO reminder in _model.ReturnReminders)
            {
                _signals.Schedule.Dispatch(new NotificationRequestVO
                {
                    Key = reminder.Notification,
                    Tag = reminder.Tag,
                    After = TimeSpan.FromMinutes(reminder.AfterMinutes)
                });
            }
        }
    }
}
