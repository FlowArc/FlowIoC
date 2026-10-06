using System;
using FlowIoC.BaseModule.Signals;
using Modules.CameraModule.Shared.Data.ValueObjects;
using Modules.CameraModule.Shared.Enums;
using UnityEngine;

namespace Modules.CameraModule.Signals
{
    /// <summary>
    /// What the module says to its own commands: the adapters hand their cameras over, and
    /// <see cref="Services.CameraService"/> turns each call into the step that does it, so every
    /// change is a step the Flow Console shows. Nothing outside the module dispatches these - a
    /// game reaches the module through ICameraService.
    /// </summary>
    internal class CameraInternalSignals : ISignalHolder
    {
        public Signal<CameraName, CameraCVO> RegisterCamera = new();
        public Signal<CameraName> UnregisterCamera = new();

        public Signal<CameraName> SwitchCamera = new();
        public Signal<Transform> SetCameraTarget = new();
        public Signal<CameraName> RememberPosition = new();
        public Signal<Vector3, float, Action> MoveCamera = new();
        public Signal<float, float, Action> SetDistance = new();
    }
}
