#if UNITY_EDITOR

using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.CameraModule.CameraTestModule.Models;
using Modules.CameraModule.Services;
using Modules.CameraModule.Shared.Enums;

namespace Modules.CameraModule.CameraTestModule.Controllers
{
    /// <summary>What a game's Command does when it decided the other camera goes live: A to B, B to A.</summary>
    internal class SwitchTestCameraCommand : Command
    {
        [Inject] private ICameraService _cameras { get; set; }
        [Inject] private ICameraTestModel _model { get; set; }

        public override void Execute()
        {
            CameraName next = _model.Live == CameraName.Menu ? CameraName.Gameplay : CameraName.Menu;

            _cameras.Switch(next);
            _model.SetLive(next);
        }
    }
}

#endif
