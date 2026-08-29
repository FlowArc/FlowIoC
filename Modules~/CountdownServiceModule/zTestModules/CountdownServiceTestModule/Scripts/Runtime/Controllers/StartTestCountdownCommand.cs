#if UNITY_EDITOR
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.CountdownServiceModule.Services;
using Modules.CountdownServiceModule.CountdownServiceTestModule.Models;
using Modules.CountdownServiceModule.CountdownServiceTestModule.Signals;

namespace Modules.CountdownServiceModule.CountdownServiceTestModule.Controllers
{
    /// <summary>
    /// Starts the test countdown. This is where a game meets the countdown service: the command
    /// injects ICountdownService directly - the one thing a module may reference across a module
    /// boundary - and turns its callbacks back into signals this module announces.
    /// </summary>
    internal class StartTestCountdownCommand : Command
    {
        [Inject] private ICountdownService _countdownService { get; set; }
        [Inject] private ICountdownTestModel _testModel { get; set; }
        [InjectSignal] private CountdownServiceTestSignals _signals { get; set; }

        public override void Execute()
        {
            // CountDownFrom takes a start time, so it works whether or not the service is up
            // yet. checkActive answers now and again later if the service was still starting.
            _countdownService.CountDownFrom(
                _testModel.CountdownId,
                _testModel.Duration,
                _countdownService.GetTime() ?? System.DateTime.UtcNow,
                checkActive: isActive => _signals.Outgoing.ServiceActive.Dispatch(isActive),
                countdownTick: remaining => _signals.Outgoing.Ticked.Dispatch(remaining),
                countdownComplete: () => _signals.Outgoing.Completed.Dispatch(),
                countdownStop: () => _signals.Outgoing.Stopped.Dispatch(),
                elapsedTimeTick: elapsed => _signals.Outgoing.Elapsed.Dispatch(elapsed));
        }
    }
}
#endif
