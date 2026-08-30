using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.CameraSystemModule.Shared.Data.ValueObjects;
using Modules.CameraSystemModule.Models.Runtime;
using Modules.CameraSystemModule.Shared.Enums;

namespace Modules.CameraSystemModule.Controllers
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
