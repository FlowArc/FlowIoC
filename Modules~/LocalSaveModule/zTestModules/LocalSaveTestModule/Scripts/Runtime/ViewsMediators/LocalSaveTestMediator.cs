#if UNITY_EDITOR

using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using Modules.LocalSaveModule.LocalSaveTestModule.Signals;

namespace Modules.LocalSaveModule.LocalSaveTestModule.ViewsMediators
{
    public class LocalSaveTestMediator : IMediator
    {
        [Inject] private LocalSaveTestView _view { get; set; }

        [InjectSignal] private LocalSaveTestSignals _signals { get; set; }

        public void OnRegister()
        {
            _view.OnIncrementPressed += Increment;
            _signals.Outgoing.CounterChanged.AddListener(OnCounterChanged);
        }

        public void OnRemove()
        {
            _view.OnIncrementPressed -= Increment;
            _signals.Outgoing.CounterChanged.RemoveListener(OnCounterChanged);
        }

        private void Increment() => _signals.Incoming.IncrementProbe.Dispatch();

        private void OnCounterChanged(int counter) => _view.SetCounter($"Counter: {counter}");
    }
}

#endif
