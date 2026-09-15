using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.MobileNotificationModule.Models;
using Modules.MobileNotificationModule.Services;

namespace Modules.MobileNotificationModule.Controllers
{
    /// <summary>Takes back everything scheduled, the return reminders included.</summary>
    internal class CancelAllNotificationsCommand : Command
    {
        [Inject] private IMobileNotificationModel _model { get; set; }
        [Inject] private INotificationGateway _gateway { get; set; }

        public override void Execute()
        {
            _gateway.CancelAllScheduled();
            _model.ClearScheduled();
            FlowLogger.Log("CancelAll");
        }
    }
}
