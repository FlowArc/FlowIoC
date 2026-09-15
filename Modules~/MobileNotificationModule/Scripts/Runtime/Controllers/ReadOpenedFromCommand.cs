using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.MobileNotificationModule.Data.ValueObjects;
using Modules.MobileNotificationModule.Models;
using Modules.MobileNotificationModule.Services;

namespace Modules.MobileNotificationModule.Controllers
{
    /// <summary>
    /// Reads which notification, if any, the player tapped to open the app, into the model. The
    /// platform keeps reporting the same one until another is tapped, so the value stays until
    /// then and is logged only when it changes.
    /// </summary>
    internal class ReadOpenedFromCommand : Command
    {
        [Inject] private IMobileNotificationModel _model { get; set; }
        [Inject] private INotificationGateway _gateway { get; set; }

        public override void Execute()
        {
            string identifier = _gateway.ReadOpenedFrom();

            if (string.IsNullOrEmpty(identifier))
                return;

            var identity = new NotificationIdentityVO(identifier);

            if (identity.Key == _model.OpenedFromKey && identity.Tag == _model.OpenedFromTag)
                return;

            _model.SetOpenedFrom(identity);
            FlowLogger.Log("Opened from " + identifier);
        }
    }
}
