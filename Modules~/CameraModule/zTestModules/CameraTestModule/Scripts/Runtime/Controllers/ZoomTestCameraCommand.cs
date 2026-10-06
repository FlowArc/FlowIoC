#if UNITY_EDITOR

using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.CameraModule.CameraTestModule.Models;
using Modules.CameraModule.Services;
using UnityEngine;

namespace Modules.CameraModule.CameraTestModule.Controllers
{
    /// <summary>Zooms the live camera to a random distance in the scene's range, and holds the step until it gets there.</summary>
    internal class ZoomTestCameraCommand : Command
    {
        [Inject] private ICameraService _cameras { get; set; }
        [Inject] private ICameraTestModel _model { get; set; }

        public override void Execute()
        {
            float distance = Random.Range(_model.MinDistance, _model.MaxDistance);

            Retain();
            _cameras.SetDistance(distance, _model.MoveSeconds, () => Release());
        }
    }
}

#endif
