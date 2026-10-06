#if UNITY_EDITOR

using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using Modules.CameraModule.CameraTestModule.Signals;

namespace Modules.CameraModule.CameraTestModule.ViewsMediators
{
    public class CameraTestMediator : IMediator
    {
        [Inject] private CameraTestView _view { get; set; }

        [InjectSignal] private CameraTestInternalSignals _signals { get; set; }

        public void OnRegister()
        {
            _view.OnSwitchPressed += Switch;
            _view.OnMovePressed += Move;
            _view.OnZoomPressed += Zoom;
            _signals.StateChanged.AddListener(_view.SetStatus);
        }

        public void OnRemove()
        {
            _view.OnSwitchPressed -= Switch;
            _view.OnMovePressed -= Move;
            _view.OnZoomPressed -= Zoom;
            _signals.StateChanged.RemoveListener(_view.SetStatus);
        }

        private void Switch() => _signals.SwitchRequested.Dispatch();

        private void Move() => _signals.MoveRequested.Dispatch();

        private void Zoom() => _signals.ZoomRequested.Dispatch();
    }
}

#endif

