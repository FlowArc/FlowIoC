using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.MobileNotificationModule.Data.ValueObjects;
using Modules.MobileNotificationModule.Models;
using Modules.MobileNotificationModule.Services;

namespace Modules.MobileNotificationModule.Controllers
{
    /// <summary>Takes one identity back from the platform and forgets it. Cancelling what is not scheduled is not an error.</summary>
    internal class CancelNotificationCommand : Command
    {
        [Inject] private IMobileNotificationModel _model { get; set; }
        [Inject] private INotificationGateway _gateway { get; set; }

        [SignalParam] private NotificationRequestVO _request { get; set; }

        public override void Execute()
        {
            var identity = new NotificationIdentityVO(_request.Key, _request.Tag);

            _gateway.Cancel(identity);
            _model.MarkCancelled(identity.Identifier);
            FlowLogger.Log("Cancel - " + identity.Identifier);
        }
    }
}
