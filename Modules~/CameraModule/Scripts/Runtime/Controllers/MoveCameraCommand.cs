using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.Provider.Update;
using FlowIoC.ConsoleModule;
using Modules.CameraModule.Models.Runtime;
using UnityEngine;

namespace Modules.CameraModule.Controllers
{
    /// <summary>
    /// Moves the live camera by moving what it follows to a point, over the seconds given, and
    /// holds its step until it arrives. Nothing to follow is reported and ends the step at once,
    /// so a sequence never waits on a move that cannot happen.
    /// </summary>
    internal class MoveCameraCommand : Command
    {
        [SignalParam] private Vector3 _position { get; set; }
        [SignalParam] private float _seconds { get; set; }
        [SignalParam] private Action _arrived { get; set; }

        [Inject] private ICameraModel _cameraModel { get; set; }
        [Inject] private IUpdateProvider _updateProvider { get; set; }

        private Transform _target;
        private Vector3 _from;
        private Vector3 _to;
        private float _duration;
        private float _elapsed;
        private Action _onArrived;
        private IUpdateProvider _frames;

        public override void Execute()
        {
            Transform target = _cameraModel.GetActiveCamera() != null ? _cameraModel.GetActiveCamera().Follow : null;

            if (target == null)
            {
                FlowLogger.LogError("MoveCameraCommand - the live camera follows nothing, so there is nothing to move.");
                _arrived?.Invoke();
                return;
            }

            if (_seconds <= 0f)
            {
                target.position = _position;
                _arrived?.Invoke();
                return;
            }

            // Everything the frames read is kept here: the injections go when the step ends.
            _target = target;
            _from = target.position;
            _to = _position;
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

                if (_target != null)
                    _target.position = Vector3.Lerp(_from, _to, Mathf.SmoothStep(0f, 1f, t));

                if (t < 1f && _target != null)
                    return;
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"MoveCameraCommand - a frame of the move threw: {exception}");
            }

            Finish();
        }

        private void Finish()
        {
            _frames.RemoveUpdate(Step);

            Action arrived = _onArrived;
            _onArrived = null;
            _target = null;

            Release();
            arrived?.Invoke();
        }
    }
}
