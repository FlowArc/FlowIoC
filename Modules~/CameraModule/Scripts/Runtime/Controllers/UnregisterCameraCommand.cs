using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.CameraModule.Models.Runtime;
using Modules.CameraModule.Shared.Enums;

namespace Modules.CameraModule.Controllers
{
    public class UnregisterCameraCommand : Command
    {
        [SignalParam] private CameraName _cameraId { get; set; }
        [Inject] private ICameraModel _cameraModel { get; set; }

        public override void Execute()
        {
            _cameraModel.UnregisterCamera(_cameraId);
        }
    }
}
