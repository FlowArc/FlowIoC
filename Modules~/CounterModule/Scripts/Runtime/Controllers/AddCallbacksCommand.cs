using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.CounterModule.Data.ValueObjects;
using Modules.CounterModule.Models;

namespace Modules.CounterModule.Controllers
{
    /// <summary>
    /// Subscribes one caller's callbacks to a counter that is already in the model. This runs
    /// both behind a fresh start and on its own, which is what lets several callers share one id.
    /// </summary>
    internal class AddCallbacksCommand : Command
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
                        $"{nameof(AddCallbacksCommand)} - no counter is running with id '{_request.Id}'.");

                    // The sequence ends here rather than carrying on to report a first value for
                    // a counter that does not exist.
                    Stop();
                    return;
                }

                if (_request.CounterTick != null)
                {
                    if (_request.IsPercentageTick)
                        counter.TickPercentageCallbacks.Add(_request.CounterTick);
                    else
                        counter.TickCallbacks.Add(_request.CounterTick);
                }

                if (_request.ElapsedTimeTick != null)
                    counter.TickElapsedTimeCallbacks.Add(_request.ElapsedTimeTick);

                if (_request.CounterComplete != null)
                    counter.CompleteCallbacks.Add(_request.CounterComplete);

                if (_request.CounterStop != null)
                    counter.StopCallbacks.Add(_request.CounterStop);

                if (_request.CheckActive != null)
                    counter.CheckActiveCallbacks.Add(_request.CheckActive);
            }

            Release();
        }
    }
}
