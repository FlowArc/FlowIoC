using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.ConsoleModule;
using Modules.CameraModule.Shared.Data.ValueObjects;
using Modules.CameraModule.Shared.Enums;
using Modules.CameraModule.Shared.Signals;
using UnityEngine.Rendering;

namespace Modules.CameraModule.ViewsMediators
{
    public class CameraAdapterMediator : IMediator
    {
        [Inject] private CameraAdapterView _view { get; set; }
        [InjectSignal] private CameraSignals _cameraSignals { get; set; }

        public void OnRegister()
        {
            RegisterCameras();
            _cameraSignals.Incoming.PublishCameraTarget.AddListener(PublishCameraTarget);
            PublishCameraTarget();
            _view.OnUnregisterCameras += UnregisterCameras;
        }

        public void OnRemove()
        {
            _cameraSignals.Incoming.PublishCameraTarget.RemoveListener(PublishCameraTarget);
            _view.OnUnregisterCameras -= UnregisterCameras;
        }

        private void RegisterCameras()
        {
            var configs = _view.GetCameraConfigs();
            if (configs == null || configs.Count == 0)
            {
                FlowLogger.LogError(FlowLogType.CameraModule, "[CameraAdapterMediator]: No camera configurations found to register.");
                return;
            }

            foreach (var kvp in configs)
            {
                _cameraSignals.Incoming.RegisterCamera.Dispatch(kvp.Key, kvp.Value);
            }
        }

        private void PublishCameraTarget()
        {
            var target = _view.GetCameraTarget();
            if (target == null)
            {
                FlowLogger.LogWarning(FlowLogType.CameraModule, "[CameraAdapterMediator]: No camera target assigned.");
                return;
            }

            _cameraSignals.Outgoing.CameraTargetReady.Dispatch(target);
        }

        private void UnregisterCameras(SerializedDictionary<CameraName, CameraCVO> configs)
        {
            if (configs == null)
                return;

            foreach (var kvp in configs)
            {
                _cameraSignals.Incoming.UnregisterCamera.Dispatch(kvp.Key);
            }
        }
    }
}
