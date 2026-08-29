using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.CountdownServiceModule.Data.ValueObjects;

namespace Modules.CountdownServiceModule.Controllers
{
    /// <summary>
    /// Gives a caller that has just subscribed its first value straight away, instead of leaving
    /// a label empty until the next second comes round.
    /// </summary>
    internal class TickProcessForNewDataCommand : TickProcessBaseCommand
    {
        [SignalParam] private CountdownRequestVO _request { get; set; }

        public override void Execute()
        {
            // Nothing is ticking yet, so there is no first value to give. The caller hears
            // through its checkActive callback once the module comes up.
            if (!_countdownModel.IsActive)
                return;

            CountdownVO countdown;

            lock (_countdownModel.LockObject)
            {
                if (!_countdownModel.DataMap.TryGetValue(_request.Id, out countdown))
                    return;
            }

            int elapsed = ElapsedSeconds(countdown);

            if (_request.CountdownTick != null)
            {
                int seconds = countdown.IsElapsedOnly ? elapsed : RemainingSeconds(countdown);

                _request.CountdownTick.Invoke(_request.IsPercentageTick
                    ? RemainingFraction(countdown, seconds)
                    : seconds);
            }

            _request.ElapsedTimeTick?.Invoke(elapsed);
        }
    }
}
