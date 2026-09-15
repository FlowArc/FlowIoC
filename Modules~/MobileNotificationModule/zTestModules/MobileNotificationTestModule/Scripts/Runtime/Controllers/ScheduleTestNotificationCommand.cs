#if UNITY_EDITOR

using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.MobileNotificationModule.Services;

namespace Modules.MobileNotificationModule.MobileNotificationTestModule.Controllers
{
    /// <summary>The Test template, ten seconds out, tagged with the button that asked; the body shows when it was asked.</summary>
    public class ScheduleTestNotificationCommand : Command
    {
        [Inject] private IMobileNotificationService _notifications { get; set; }

        [SignalParam] private string _tag { get; set; }

        public override void Execute() =>
            _notifications.Schedule("Test", TimeSpan.FromSeconds(10), _tag, DateTime.Now.ToString("HH:mm:ss"));
    }
}

#endif
