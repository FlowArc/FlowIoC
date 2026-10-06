#if UNITY_EDITOR

using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.CameraModule.CameraTestModule.Models;
using Modules.CameraModule.Services;
using UnityEngine;

namespace Modules.CameraModule.CameraTestModule.Controllers
{
    /// <summary>
    /// Glides the live camera to a random point in a circle around its cube - the position the
    /// scene remembered for it at launch - and holds the step until it arrives.
    /// </summary>
    internal class MoveTestCameraCommand : Command
    {
        [Inject] private ICameraService _cameras { get; set; }
        [Inject] private ICameraTestModel _model { get; set; }

        public override void Execute()
        {
            if (!_cameras.TryGetRememberedPosition(_model.Live, out Vector3 home))
            {
                FlowLogger.LogError($"MoveTestCameraCommand - no position was remembered for {_model.Live}.");
                return;
            }

            Vector2 offset = Random.insideUnitCircle * _model.MoveRadius;
            Vector3 point = home + new Vector3(offset.x, 0f, offset.y);

            Retain();
            _cameras.MoveCamera(point, _model.MoveSeconds, () => Release());
        }
    }
}

#endif
