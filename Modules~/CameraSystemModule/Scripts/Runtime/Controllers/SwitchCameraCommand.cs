using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.CameraSystemModule.Models.Runtime;
using Modules.CameraSystemModule.Shared.Enums;

namespace Modules.CameraSystemModule.Controllers
{
    public class SwitchCameraCommand : Command
    {
        [SignalParam] private CameraName _cameraId { get; set; }
        [Inject] private ICameraModel _cameraModel { get; set; }

        public override void Execute()
        {
            _cameraModel.SetActiveCamera(_cameraId);
        }
    }
}
