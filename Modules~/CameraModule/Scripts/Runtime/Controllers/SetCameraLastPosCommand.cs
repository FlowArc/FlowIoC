using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.CameraModule.Models.Runtime;
using Modules.CameraModule.Shared.Enums;

namespace Modules.CameraModule.Controllers
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
