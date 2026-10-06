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
            /// Remembers the live camera's position under the camera given where the step is
            /// bound, before the flow leaves it:
            /// <c>.ToSequence&lt;ICameraService.Commands.RememberPosition&gt;(CameraName.Gameplay)</c>.
            /// </summary>
            public class RememberPosition : Command<CameraName>
            {
                [Inject] private ICameraService _cameras { get; set; }

                public override void Execute(CameraName camera) => _cameras.RememberPosition(camera);
            }
        }
    }
}
