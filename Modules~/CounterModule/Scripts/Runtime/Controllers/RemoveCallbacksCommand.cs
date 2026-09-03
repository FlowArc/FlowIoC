using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.CounterModule.Data.ValueObjects;
using Modules.CounterModule.Models;

namespace Modules.CounterModule.Controllers
{
    /// <summary>
    /// Unsubscribes one caller from a counter, and drops the counter itself once nothing is
    /// left listening to it - so a screen that unsubscribes as it closes leaves nothing running.
    /// </summary>
    internal class RemoveCallbacksCommand : Command
    {
        [SignalParam] private CounterRequestVO _request { get; set; }
        [Inject] private ICounterModel _counterModel { get; set; }

        public override void Execute()
        {
            Retain();

            lock (_counterModel.LockObject)
            {
                if (!_counterModel.DataMap.TryGetValue(_request.Id, out CounterVO counter))
                {
                    FlowLogger.LogError(FlowLogType.CounterModule,
                        $"{nameof(RemoveCallbacksCommand)} - no counter is running with id '{_request.Id}'.");

                    Release();
                    return;
                }

                if (_request.CounterTick != null)
                {
                    // Which list it went into depended on a flag the caller passed when it
                    // subscribed. Removing from both spares the caller from having to remember.
                    counter.TickCallbacks.Remove(_request.CounterTick);
                    counter.TickPercentageCallbacks.Remove(_request.CounterTick);
                }

                if (_request.ElapsedTimeTick != null)
                    counter.TickElapsedTimeCallbacks.Remove(_request.ElapsedTimeTick);

                if (_request.CounterComplete != null)
                    counter.CompleteCallbacks.Remove(_request.CounterComplete);

                if (_request.CounterStop != null)
                    counter.StopCallbacks.Remove(_request.CounterStop);

                if (counter.HasNoListeners)
                {
                    _counterModel.DataMap.Remove(_request.Id);
                    counter.Clear();
                }
            }

            Release();
        }
    }
}
