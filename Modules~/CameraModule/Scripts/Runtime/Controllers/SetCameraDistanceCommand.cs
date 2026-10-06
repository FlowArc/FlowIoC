using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.Provider.Update;
using FlowIoC.ConsoleModule;
using Modules.CameraModule.Models.Runtime;
using Unity.Cinemachine;
using UnityEngine;

namespace Modules.CameraModule.Controllers
{
    /// <summary>
    /// Zooms the live camera: eases its position composer's distance to the one given, over the
    /// seconds given, and holds its step until it gets there. A live camera without a position
    /// composer is reported and ends the step at once, so a sequence never waits on a zoom that
    /// cannot happen.
    /// </summary>
    internal class SetCameraDistanceCommand : Command
    {
        [SignalParam] private float _distance { get; set; }
        [SignalParam] private float _seconds { get; set; }
        [SignalParam] private Action _arrived { get; set; }

        [Inject] private ICameraModel _cameraModel { get; set; }
        [Inject] private IUpdateProvider _updateProvider { get; set; }

        private CinemachinePositionComposer _composer;
        private float _from;
        private float _to;
        private float _duration;
        private float _elapsed;
        private Action _onArrived;
        private IUpdateProvider _frames;

        public override void Execute()
        {
            CinemachineCamera live = _cameraModel.GetActiveCamera();
            CinemachinePositionComposer composer = live != null ? live.GetComponent<CinemachinePositionComposer>() : null;

            if (composer == null)
            {
                FlowLogger.LogError("SetCameraDistanceCommand - the live camera has no CinemachinePositionComposer, so it has no distance to change.");
                _arrived?.Invoke();
                return;
            }

            if (_seconds <= 0f)
            {
                composer.CameraDistance = _distance;
                _arrived?.Invoke();
                return;
            }

            // Everything the frames read is kept here: the injections go when the step ends.
            _composer = composer;
            _from = composer.CameraDistance;
            _to = _distance;
            _duration = _seconds;
            _elapsed = 0f;
            _onArrived = _arrived;
            _frames = _updateProvider;

            Retain();
            _frames.AddUpdate(Step);
        }

        private void Step()
        {
            try
            {
                _elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(_elapsed / _duration);

                if (_composer != null)
                    _composer.CameraDistance = Mathf.Lerp(_from, _to, Mathf.SmoothStep(0f, 1f, t));

                if (t < 1f && _composer != null)
                    return;
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"SetCameraDistanceCommand - a frame of the zoom threw: {exception}");
            }

            Finish();
        }

        private void Finish()
        {
            _frames.RemoveUpdate(Step);

            Action arrived = _onArrived;
            _onArrived = null;
            _composer = null;

            Release();
            arrived?.Invoke();
        }
    }
}
