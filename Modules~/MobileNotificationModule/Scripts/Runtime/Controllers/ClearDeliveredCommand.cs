using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.MobileNotificationModule.Services;

namespace Modules.MobileNotificationModule.Controllers
{
    /// <summary>The player is here: the tray and the badge have nothing left to say.</summary>
    internal class ClearDeliveredCommand : Command
    {
        [Inject] private INotificationGateway _gateway { get; set; }

        public override void Execute() => _gateway.ClearDelivered();
    }
}
