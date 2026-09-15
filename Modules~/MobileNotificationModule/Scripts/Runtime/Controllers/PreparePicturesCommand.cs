using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.MobileNotificationModule.Models;
using Modules.MobileNotificationModule.Services;

namespace Modules.MobileNotificationModule.Controllers
{
    /// <summary>
    /// Hands the catalogue's pictures to the platform once, from the Initialize sequence, so they
    /// are where a notification can carry them by the time one is scheduled. Android copies them
    /// out of the APK; the others read them in place.
    /// </summary>
    internal class PreparePicturesCommand : Command
    {
        [Inject] private IMobileNotificationModel _model { get; set; }
        [Inject] private INotificationGateway _gateway { get; set; }

        public override void Execute()
        {
            if (_model.Pictures.Count == 0)
                return;

            _gateway.PreparePictures(_model.Pictures);
            FlowLogger.Log($"PreparePictures - {_model.Pictures.Count} picture(s)");
        }
    }
}
