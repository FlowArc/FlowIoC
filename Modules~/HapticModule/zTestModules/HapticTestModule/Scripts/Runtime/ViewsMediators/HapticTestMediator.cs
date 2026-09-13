#if UNITY_EDITOR

using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using Modules.HapticModule.Enums;
using Modules.HapticModule.HapticTestModule.Data.ValueObjects;
using Modules.HapticModule.HapticTestModule.Signals;

namespace Modules.HapticModule.HapticTestModule.ViewsMediators
{
    public class HapticTestMediator : IMediator
    {
        [Inject] private HapticTestView _view { get; set; }

        [InjectSignal] private HapticTestInternalSignals _signals { get; set; }

        public void OnRegister()
        {
            _view.OnPresetPressed += Play;
            _view.OnEnabledChanged += SetEnabled;
            _signals.StateChanged.AddListener(OnStateChanged);
        }

        public void OnRemove()
        {
            _view.OnPresetPressed -= Play;
            _view.OnEnabledChanged -= SetEnabled;
            _signals.StateChanged.RemoveListener(OnStateChanged);
        }

        private void Play(HapticPreset preset) => _signals.PlayRequested.Dispatch(preset);

        private void SetEnabled(bool on) => _signals.EnabledChangeRequested.Dispatch(on);

        private void OnStateChanged(HapticTestStateVO state)
        {
            _view.SetEnabledWithoutNotify(state.IsEnabled);
            _view.SetStatus($"{state.Platform} - haptics {(state.IsEnabled ? "on" : "off")}");
        }
    }
}

#endif
