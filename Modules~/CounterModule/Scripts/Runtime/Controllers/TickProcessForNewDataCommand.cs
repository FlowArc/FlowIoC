using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.CounterModule.Data.ValueObjects;

namespace Modules.CounterModule.Controllers
{
    /// <summary>
    /// Gives a caller that has just subscribed its first value straight away, instead of leaving
    /// a label empty until the next second comes round.
    /// </summary>
    internal class TickProcessForNewDataCommand : TickProcessBaseCommand
    {
        [SignalParam] private CounterRequestVO _request { get; set; }

        public override void Execute()
        {
            // Nothing is ticking yet, so there is no first value to give. The caller hears
            // through its checkActive callback once the module comes up.
            if (!_counterModel.IsActive)
                return;

            CounterVO counter;

            lock (_counterModel.LockObject)
            {
                if (!_counterModel.DataMap.TryGetValue(_request.Id, out counter))
                    return;
            }

            int elapsed = ElapsedSeconds(counter);

            if (_request.CounterTick != null)
            {
                int seconds = counter.IsElapsedOnly ? elapsed : RemainingSeconds(counter);

                _request.CounterTick.Invoke(_request.IsPercentageTick
                    ? RemainingFraction(counter, seconds)
                    : seconds);
            }

            _request.ElapsedTimeTick?.Invoke(elapsed);
        }
    }
}
