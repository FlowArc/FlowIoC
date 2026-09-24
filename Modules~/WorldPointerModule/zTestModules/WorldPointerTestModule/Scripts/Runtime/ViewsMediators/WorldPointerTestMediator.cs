#if UNITY_EDITOR
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using Modules.WorldPointerModule.WorldPointerTestModule.Signals;

namespace Modules.WorldPointerModule.WorldPointerTestModule.ViewsMediators
{
    /// <summary>
    /// Drives one view. Button presses leave as signals carrying what the view holds, and the
    /// count comes back as one - nothing about a pointer is decided here.
    /// </summary>
    public class WorldPointerTestMediator : IMediator
    {
        [Inject] private WorldPointerTestView _view { get; set; }
        [InjectSignal] private WorldPointerTestInternalSignals _signals { get; set; }

        public void OnRegister()
        {
            _view.OnRegisterPressed += RegisterPointers;
            _view.OnChangeContentPressed += ChangeContent;
            _view.OnHiddenToggled += SetHidden;
            _view.OnToggleScreenPressed += ToggleScreen;
            _view.OnUnregisterAllPressed += UnregisterPointers;

            _signals.PointerCountChanged.AddListener(OnPointerCountChanged);

            _view.SetCount("0 targets");
        }

        public void OnRemove()
        {
            _view.OnRegisterPressed -= RegisterPointers;
            _view.OnChangeContentPressed -= ChangeContent;
            _view.OnHiddenToggled -= SetHidden;
            _view.OnToggleScreenPressed -= ToggleScreen;
            _view.OnUnregisterAllPressed -= UnregisterPointers;

            _signals.PointerCountChanged.RemoveListener(OnPointerCountChanged);
        }

        private void RegisterPointers() => _signals.RegisterPointers.Dispatch(_view.Targets);

        private void ChangeContent() => _signals.ChangeContent.Dispatch(_view.Targets);

        private void SetHidden(bool hidden) => _signals.SetHidden.Dispatch(_view.Targets, hidden);

        private void ToggleScreen() => _signals.ToggleSampleScreen.Dispatch();

        private void UnregisterPointers() => _signals.UnregisterPointers.Dispatch();

        private void OnPointerCountChanged(int count) => _view.SetCount($"{count} targets");
    }
}
#endif