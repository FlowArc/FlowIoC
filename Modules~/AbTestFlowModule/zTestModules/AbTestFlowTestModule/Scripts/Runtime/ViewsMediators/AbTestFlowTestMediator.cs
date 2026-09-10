#if UNITY_EDITOR

using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using Modules.AbTestFlowModule.AbTestFlowTestModule.Data.ValueObjects;
using Modules.AbTestFlowModule.AbTestFlowTestModule.Signals;

namespace Modules.AbTestFlowModule.AbTestFlowTestModule.ViewsMediators
{
    public class AbTestFlowTestMediator : IMediator
    {
        [Inject] private AbTestFlowTestView _view { get; set; }

        [InjectSignal] private AbTestFlowTestInternalSignals _signals { get; set; }

        public void OnRegister()
        {
            _view.OnClearPressed += Clear;
            _view.OnRaiseVersionPressed += RaiseVersion;
            _view.OnRerollPressed += Reroll;
            _signals.StateChanged.AddListener(OnStateChanged);
        }

        public void OnRemove()
        {
            _view.OnClearPressed -= Clear;
            _view.OnRaiseVersionPressed -= RaiseVersion;
            _view.OnRerollPressed -= Reroll;
            _signals.StateChanged.RemoveListener(OnStateChanged);
        }

        private void Clear() => _signals.ClearStoredAbTest.Dispatch();

        private void RaiseVersion() => _signals.RaiseVersion.Dispatch();

        private void Reroll() => _signals.Reroll.Dispatch();

        private void OnStateChanged(AbTestFlowTestStateVO state)
        {
            string group = state.Group ?? "outside the test";

            _view.SetExperiment($"{state.AbTestId} v{state.Version}: {group}");
            _view.SetProbe($"Probe - Lives {state.Lives}, Speed {state.Speed:0.##}");
        }
    }
}

#endif
