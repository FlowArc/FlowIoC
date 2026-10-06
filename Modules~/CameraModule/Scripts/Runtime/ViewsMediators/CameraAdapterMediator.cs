using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.ConsoleModule;
using Modules.CameraModule.Shared.Data.ValueObjects;
using Modules.CameraModule.Shared.Enums;
using Modules.CameraModule.Signals;
using UnityEngine.Rendering;

namespace Modules.CameraModule.ViewsMediators
{
    /// <summary>
    /// Hands the rig's cameras to the module when it comes up and takes them back when it goes.
    /// </summary>
    public class CameraAdapterMediator : IMediator
    {
        [Inject] private CameraAdapterView _view { get; set; }
        [InjectSignal] private CameraInternalSignals _signals { get; set; }

        public void OnRegister()
        {
            RegisterCameras();
            _view.OnUnregisterCameras += UnregisterCameras;
        }

        public void OnRemove()
        {
            _view.OnUnregisterCameras -= UnregisterCameras;
        }

        private void RegisterCameras()
        {
            var configs = _view.GetCameraConfigs();
            if (configs == null || configs.Count == 0)
            {
                FlowLogger.LogError("[CameraAdapterMediator]: No camera configurations found to register.");
                return;
            }

            foreach (var kvp in configs)
            {
                _signals.RegisterCamera.Dispatch(kvp.Key, kvp.Value);
            }
        }

        private void UnregisterCameras(SerializedDictionary<CameraName, CameraCVO> configs)
        {
            if (configs == null)
                return;

            foreach (var kvp in configs)
            {
                _signals.UnregisterCamera.Dispatch(kvp.Key);
            }
        }
    }
}
