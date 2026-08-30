using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.CameraSystemModule.Models.Runtime;
using UnityEngine;

namespace Modules.CameraSystemModule.Controllers
{
    public class SetCameraTargetCommand : Command
    {
        [SignalParam] private Transform _target { get; set; }
        [Inject] private ICameraModel _cameraModel { get; set; }

        public override void Execute()
        {
            var activeCamera = _cameraModel.GetActiveCamera();

            if (activeCamera == null || _target == null)
            {
                FlowLogger.LogError(FlowLogType.CameraSystemModule, "[SetCameraTargetCommand]: Active camera or target is null.");
                return;
            }

            activeCamera.Follow = _target;
            activeCamera.LookAt = _target;
        }
    }
}
