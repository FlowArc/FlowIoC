using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.CameraSystemModule.Models.Runtime;
using Modules.CameraSystemModule.Shared.Enums;

namespace Modules.CameraSystemModule.Controllers
{
    public class SetCameraLastPosCommand : Command
    {
        [SignalParam] private CameraName _type { get; set; }
        [Inject] private ICameraModel _cameraModel { get; set; }

        public override void Execute()
        {
            _cameraModel.SetCameraLastPos(_type, _cameraModel.GetActiveCamera().Follow.position);
        }
    }
}
