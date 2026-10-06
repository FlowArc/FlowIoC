using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using UnityEngine;

namespace Modules.CameraModule.Services
{
    public partial interface ICameraService
    {
        public static partial class Commands
        {
            /// <summary>
            /// Moves the live camera to the point, over the seconds, given where the step is bound,
            /// and holds the sequence until it arrives:
            /// <c>.ToSequence&lt;ICameraService.Commands.MoveCamera&gt;(new Vector3(0f, 12f, -8f), 0.6f)</c>.
            /// A point decided at runtime is a game Command injecting ICameraService.
            /// </summary>
            public class MoveCamera : Command<Vector3, float>
            {
                [Inject] private ICameraService _cameras { get; set; }

                public override void Execute(Vector3 position, float seconds)
                {
                    Retain();

                    try
                    {
                        _cameras.MoveCamera(position, seconds, () => Release());
                    }
                    catch (Exception exception)
                    {
                        // A throw before the move could end would hold the sequence for ever.
                        FlowLogger.LogError($"ICameraService.Commands.MoveCamera - {position} threw: {exception}");
                        Release();
                    }
                }
            }
        }
    }
}
