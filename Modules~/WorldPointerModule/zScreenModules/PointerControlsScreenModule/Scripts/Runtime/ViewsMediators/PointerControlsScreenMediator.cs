#if UNITY_EDITOR
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.Extensions;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using Modules.WorldPointerModule.PointerControlsScreenModule.Signals;

namespace Modules.WorldPointerModule.PointerControlsScreenModule.ViewsMediators
{
    /// <summary>
    /// Presses leave on Outgoing, the count arrives on Incoming; nothing is decided here. It
    /// subscribes to the view on ShowCompleted and drops it on HideCompleted, and a press while
    /// the screen animates is not a signal.
    /// </summary>
    public class PointerControlsScreenMediator : IMediator
    {
        [Inject] private PointerControlsScreenView _view { get; set; }
        [InjectSignal] private PointerControlsScreenSignals _signals { get; set; }

        private bool _isUp => _view.Data.HasState(ScreenState.InUse);

        private bool _canSend => _view.Data.State == ScreenState.AvailableToSendSignal;

        public void OnRegister()
        {
            _view.ShowCompleted += OnScreenShown;
            _view.HideCompleted += OnScreenHidden;

            _signals.Incoming.ShowCount.AddListener(OnShowCount);
        }

        public void OnRemove()
        {
            _view.ShowCompleted -= OnScreenShown;
            _view.HideCompleted -= OnScreenHidden;

            _signals.Incoming.ShowCount.RemoveListener(OnShowCount);
        }

        private void OnScreenShown(IScreenBody screen)
        {
            _view.RegisterPressed += OnRegisterPressed;
            _view.ChangeContentPressed += OnChangeContentPressed;
            _view.HiddenToggled += OnHiddenToggled;
            _view.ToggleScreenPressed += OnToggleScreenPressed;
            _view.UnregisterAllPressed += OnUnregisterAllPressed;
        }

        private void OnScreenHidden(IScreenBody screen)
        {
            _view.RegisterPressed -= OnRegisterPressed;
            _view.ChangeContentPressed -= OnChangeContentPressed;
            _view.HiddenToggled -= OnHiddenToggled;
            _view.ToggleScreenPressed -= OnToggleScreenPressed;
            _view.UnregisterAllPressed -= OnUnregisterAllPressed;
        }

        private void OnShowCount(int count)
        {
            if (!_isUp) return;
            _view.ShowCount(count);
        }

        private void OnRegisterPressed()
        {
            if (_canSend) _signals.Outgoing.RegisterPressed.Dispatch();
        }

        private void OnChangeContentPressed()
        {
            if (_canSend) _signals.Outgoing.ChangeContentPressed.Dispatch();
        }

        private void OnHiddenToggled(bool hidden)
        {
            if (_canSend) _signals.Outgoing.HiddenToggled.Dispatch(hidden);
        }

        private void OnToggleScreenPressed()
        {
            if (_canSend) _signals.Outgoing.ToggleScreenPressed.Dispatch();
        }

        private void OnUnregisterAllPressed()
        {
            if (_canSend) _signals.Outgoing.UnregisterAllPressed.Dispatch();
        }
    }
}
#endif