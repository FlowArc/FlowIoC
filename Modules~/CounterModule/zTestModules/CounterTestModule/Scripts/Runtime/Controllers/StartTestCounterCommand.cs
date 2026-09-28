#if UNITY_EDITOR
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.CounterModule.Services;
using Modules.CounterModule.CounterTestModule.Models;
using Modules.CounterModule.CounterTestModule.Signals;

namespace Modules.CounterModule.CounterTestModule.Controllers
{
    /// <summary>
    /// Starts the test counter. This is where a game meets the counter service: the command
    /// injects ICounterService directly - the one thing a module may reference across a module
    /// boundary - and turns its callbacks back into signals this module announces.
    /// </summary>
    internal class StartTestCounterCommand : Command
    {
        [Inject] private ICounterService _counterService { get; set; }
        [Inject] private ICounterTestModel _testModel { get; set; }
        [InjectSignal] private CounterTestSignals _signals { get; set; }

        public override void Execute()
        {
            // CountDownFrom takes a start time, so it works whether or not the service is up
            // yet. checkActive answers now and again later if the service was still starting.
            // The callbacks fire after this step has ended, on a pooled instance that no longer
            // holds its injections, so the holder they dispatch on is read here.
            CounterTestSignals signals = _signals;

            _counterService.CountDownFrom(
                _testModel.CounterId,
                _testModel.Duration,
                _counterService.GetTime() ?? System.DateTime.UtcNow,
                checkActive: isActive => signals.Outgoing.ServiceActive.Dispatch(isActive),
                counterTick: remaining => signals.Outgoing.Ticked.Dispatch(remaining),
                counterComplete: () => signals.Outgoing.Completed.Dispatch(),
                counterStop: () => signals.Outgoing.Stopped.Dispatch(),
                elapsedTimeTick: elapsed => signals.Outgoing.Elapsed.Dispatch(elapsed));
        }
    }
}
#endif
