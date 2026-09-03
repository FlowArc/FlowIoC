#if UNITY_EDITOR
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using Modules.CounterModule.CounterTestModule.Signals;

namespace Modules.CounterModule.CounterTestModule.ViewsMediators
{
    /// <summary>
    /// Drives one view. Button presses leave as incoming signals, counter values arrive as
    /// outgoing ones - so no rule about what a counter is lives here either.
    /// </summary>
    public class CounterTestMediator : IMediator
    {
        [Inject] private CounterTestView _view { get; set; }
        [InjectSignal] private CounterTestSignals _signals { get; set; }

        public void OnRegister()
        {
            _view.OnStartPressed += StartCounter;
            _view.OnStopPressed += StopCounter;

            _signals.Outgoing.Ticked.AddListener(OnTicked);
            _signals.Outgoing.Elapsed.AddListener(OnElapsed);
            _signals.Outgoing.Completed.AddListener(OnCompleted);
            _signals.Outgoing.Stopped.AddListener(OnStopped);
            _signals.Outgoing.ServiceActive.AddListener(OnServiceActive);

            _view.SetStatus("Idle");
        }

        public void OnRemove()
        {
            _view.OnStartPressed -= StartCounter;
            _view.OnStopPressed -= StopCounter;

            _signals.Outgoing.Ticked.RemoveListener(OnTicked);
            _signals.Outgoing.Elapsed.RemoveListener(OnElapsed);
            _signals.Outgoing.Completed.RemoveListener(OnCompleted);
            _signals.Outgoing.Stopped.RemoveListener(OnStopped);
            _signals.Outgoing.ServiceActive.RemoveListener(OnServiceActive);
        }

        private void StartCounter() => _signals.Incoming.StartTestCounter.Dispatch();

        private void StopCounter() => _signals.Incoming.StopTestCounter.Dispatch();

        private void OnTicked(float remaining) => _view.SetRemaining($"Remaining: {remaining:0}s");

        private void OnElapsed(float elapsed) => _view.SetElapsed($"Elapsed: {elapsed:0}s");

        private void OnCompleted() => _view.SetStatus("Completed");

        private void OnStopped() => _view.SetStatus("Stopped");

        private void OnServiceActive(bool isActive) => _view.SetStatus(isActive ? "Running" : "Waiting for time source");
    }
}
#endif
