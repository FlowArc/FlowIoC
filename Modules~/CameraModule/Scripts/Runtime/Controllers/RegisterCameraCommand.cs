using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.CameraModule.Shared.Data.ValueObjects;
using Modules.CameraModule.Models.Runtime;
using Modules.CameraModule.Shared.Enums;

namespace Modules.CameraModule.Controllers
{
    public class RegisterCameraCommand : Command
    {
        [SignalParam] private CameraName _cameraId { get; set; }
        [SignalParam] private CameraCVO _config { get; set; }
        [Inject] private ICameraModel _cameraModel { get; set; }

        public override void Execute()
        {
            _cameraModel.RegisterCamera(_cameraId, _config);
        }
    }
}
