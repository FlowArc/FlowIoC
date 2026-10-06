using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;

namespace Modules.CameraModule.Services
{
    public partial interface ICameraService
    {
        public static partial class Commands
        {
            /// <summary>
            /// Zooms the live camera to the distance, over the seconds, given where the step is
            /// bound, and holds the sequence until it gets there:
            /// <c>.ToSequence&lt;ICameraService.Commands.SetDistance&gt;(6f, 0.4f)</c>.
            /// A distance decided at runtime is a game Command injecting ICameraService.
            /// </summary>
            public class SetDistance : Command<float, float>
            {
                [Inject] private ICameraService _cameras { get; set; }

                public override void Execute(float distance, float seconds)
                {
                    Retain();

                    try
                    {
                        _cameras.SetDistance(distance, seconds, () => Release());
                    }
                    catch (Exception exception)
                    {
                        // A throw before the zoom could end would hold the sequence for ever.
                        FlowLogger.LogError($"ICameraService.Commands.SetDistance - {distance} threw: {exception}");
                        Release();
                    }
                }
            }
        }
    }
}
