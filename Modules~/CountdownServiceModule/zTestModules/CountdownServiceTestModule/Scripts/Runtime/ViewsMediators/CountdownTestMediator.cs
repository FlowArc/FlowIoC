#if UNITY_EDITOR
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using Modules.CountdownServiceModule.CountdownServiceTestModule.Signals;

namespace Modules.CountdownServiceModule.CountdownServiceTestModule.ViewsMediators
{
    /// <summary>
    /// Drives one view. Button presses leave as incoming signals, countdown values arrive as
    /// outgoing ones - so no rule about what a countdown is lives here either.
    /// </summary>
    public class CountdownTestMediator : IMediator
    {
        [Inject] private CountdownTestView _view { get; set; }
        [InjectSignal] private CountdownServiceTestSignals _signals { get; set; }

        public void OnRegister()
        {
            _view.OnStartPressed += StartCountdown;
            _view.OnStopPressed += StopCountdown;

            _signals.Outgoing.Ticked.AddListener(OnTicked);
            _signals.Outgoing.Elapsed.AddListener(OnElapsed);
            _signals.Outgoing.Completed.AddListener(OnCompleted);
            _signals.Outgoing.Stopped.AddListener(OnStopped);
            _signals.Outgoing.ServiceActive.AddListener(OnServiceActive);

            _view.SetStatus("Idle");
        }

        public void OnRemove()
        {
            _view.OnStartPressed -= StartCountdown;
            _view.OnStopPressed -= StopCountdown;

            _signals.Outgoing.Ticked.RemoveListener(OnTicked);
            _signals.Outgoing.Elapsed.RemoveListener(OnElapsed);
            _signals.Outgoing.Completed.RemoveListener(OnCompleted);
            _signals.Outgoing.Stopped.RemoveListener(OnStopped);
            _signals.Outgoing.ServiceActive.RemoveListener(OnServiceActive);
        }

        private void StartCountdown() => _signals.Incoming.StartTestCountdown.Dispatch();

        private void StopCountdown() => _signals.Incoming.StopTestCountdown.Dispatch();

        private void OnTicked(float remaining) => _view.SetRemaining($"Remaining: {remaining:0}s");

        private void OnElapsed(float elapsed) => _view.SetElapsed($"Elapsed: {elapsed:0}s");

        private void OnCompleted() => _view.SetStatus("Completed");

        private void OnStopped() => _view.SetStatus("Stopped");

        private void OnServiceActive(bool isActive) => _view.SetStatus(isActive ? "Running" : "Waiting for time source");
    }
}
#endif
