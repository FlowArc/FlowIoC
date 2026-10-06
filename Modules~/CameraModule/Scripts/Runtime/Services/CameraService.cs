using System;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.CameraModule.Models.Runtime;
using Modules.CameraModule.Shared.Enums;
using Modules.CameraModule.Signals;
using Unity.Cinemachine;
using UnityEngine;

namespace Modules.CameraModule.Services
{
    /// <summary>
    /// The surface hands every change to the module's own commands through its internal signals,
    /// so each call is a step the Flow Console shows; what it answers it reads off the Model.
    /// </summary>
    public class CameraService : ICameraService
    {
        [Inject] private ICameraModel _model { get; set; }
        [InjectSignal] private CameraInternalSignals _signals { get; set; }

        public void Switch(CameraName camera) => _signals.SwitchCamera.Dispatch(camera);

        public void Follow(Transform target) => _signals.SetCameraTarget.Dispatch(target);

        public void RememberPosition(CameraName camera) => _signals.RememberPosition.Dispatch(camera);

        public void MoveCamera(Vector3 position, float seconds, Action arrived = null) =>
            _signals.MoveCamera.Dispatch(position, seconds, arrived);

        public void SetDistance(float distance, float seconds, Action arrived = null) =>
            _signals.SetDistance.Dispatch(distance, seconds, arrived);

        public bool TryGetRememberedPosition(CameraName camera, out Vector3 position) =>
            _model.TryGetCameraLastPos(camera, out position);

        public CinemachineCamera ActiveCamera => _model.GetActiveCamera();

        public bool IsRegistered(CameraName camera) => _model.TryGetCamera(camera, out _);
    }
}
