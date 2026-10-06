using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.CameraModule.Shared.Enums;

namespace Modules.CameraModule.Services
{
    public partial interface ICameraService
    {
        public static partial class Commands
        {
            /// <summary>
            /// Switches to the camera given where the step is bound:
            /// <c>.ToSequence&lt;ICameraService.Commands.Switch&gt;(CameraName.Gameplay)</c>. A step
            /// whose camera is decided at runtime is a game Command injecting ICameraService.
            /// </summary>
            public class Switch : Command<CameraName>
            {
                [Inject] private ICameraService _cameras { get; set; }

                public override void Execute(CameraName camera) => _cameras.Switch(camera);
            }
        }
    }
}
