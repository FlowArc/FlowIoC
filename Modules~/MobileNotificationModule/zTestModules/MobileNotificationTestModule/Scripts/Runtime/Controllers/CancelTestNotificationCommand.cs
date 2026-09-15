#if UNITY_EDITOR

using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.MobileNotificationModule.Services;

namespace Modules.MobileNotificationModule.MobileNotificationTestModule.Controllers
{
    public class CancelTestNotificationCommand : Command
    {
        [Inject] private IMobileNotificationService _notifications { get; set; }

        [SignalParam] private string _tag { get; set; }

        public override void Execute() => _notifications.Cancel("Test", _tag);
    }
}

#endif
