using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using Modules.CameraSystemModule.Shared.Signals;

namespace Modules.CameraSystemModule.ViewsMediators
{
    public class SingleCameraAdapterMediator : IMediator
    {
        [Inject] private SingleCameraAdapterView _view { get; set; }
        [InjectSignal] private CameraSignals _cameraSignals { get; set; }

        public void OnRegister()
        {
            RegisterCamera();
            _view.OnUnregisterCamera += UnregisterCamera;
        }

        public void OnRemove()
        {
            _view.OnUnregisterCamera -= UnregisterCamera;
        }

        private void RegisterCamera() => _cameraSignals.Incoming.RegisterCamera.Dispatch(_view.CameraKey, _view.CameraConfig);

        private void UnregisterCamera() => _cameraSignals.Incoming.UnregisterCamera.Dispatch(_view.CameraKey);
    }
}
