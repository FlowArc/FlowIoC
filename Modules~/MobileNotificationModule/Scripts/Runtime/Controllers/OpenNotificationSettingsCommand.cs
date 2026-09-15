using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.MobileNotificationModule.Services;

namespace Modules.MobileNotificationModule.Controllers
{
    /// <summary>Opens the OS notification settings for the app.</summary>
    internal class OpenNotificationSettingsCommand : Command
    {
        [Inject] private INotificationGateway _gateway { get; set; }

        public override void Execute() => _gateway.OpenSettings();
    }
}
