#if UNITY_EDITOR
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using Modules.WorldPointerModule.PointerControlsScreenModule.Signals;
using Modules.WorldPointerModule.WorldPointerTestModule.Signals;

namespace Modules.WorldPointerModule.WorldPointerTestModule.ViewsMediators
{
    /// <summary>
    /// Joins the controls screen to the test module's own commands: a press there becomes an
    /// internal signal carrying the cubes this view holds, and the count goes back to the screen.
    /// A test module may reference any module, so it listens to the screen's holder directly;
    /// nothing about a pointer is decided here.
    /// </summary>
    public class WorldPointerTestMediator : IMediator
    {
        [Inject] private WorldPointerTestView _view { get; set; }
        [InjectSignal] private WorldPointerTestInternalSignals _signals { get; set; }
        [InjectSignal] private PointerControlsScreenSignals _controls { get; set; }

        public void OnRegister()
        {
            _controls.Outgoing.RegisterPressed.AddListener(RegisterPointers);
            _controls.Outgoing.ChangeContentPressed.AddListener(ChangeContent);
            _controls.Outgoing.HiddenToggled.AddListener(SetHidden);
            _controls.Outgoing.ToggleScreenPressed.AddListener(ToggleScreen);
            _controls.Outgoing.UnregisterAllPressed.AddListener(UnregisterPointers);

            _signals.PointerCountChanged.AddListener(OnPointerCountChanged);
        }

        public void OnRemove()
        {
            _controls.Outgoing.RegisterPressed.RemoveListener(RegisterPointers);
            _controls.Outgoing.ChangeContentPressed.RemoveListener(ChangeContent);
            _controls.Outgoing.HiddenToggled.RemoveListener(SetHidden);
            _controls.Outgoing.ToggleScreenPressed.RemoveListener(ToggleScreen);
            _controls.Outgoing.UnregisterAllPressed.RemoveListener(UnregisterPointers);

            _signals.PointerCountChanged.RemoveListener(OnPointerCountChanged);
        }

        private void RegisterPointers() => _signals.RegisterPointers.Dispatch(_view.Targets);

        private void ChangeContent() => _signals.ChangeContent.Dispatch(_view.Targets);

        private void SetHidden(bool hidden) => _signals.SetHidden.Dispatch(_view.Targets, hidden);

        private void ToggleScreen() => _signals.ToggleSampleScreen.Dispatch();

        private void UnregisterPointers() => _signals.UnregisterPointers.Dispatch();

        private void OnPointerCountChanged(int count) => _controls.Incoming.ShowCount.Dispatch(count);
    }
}
#endif