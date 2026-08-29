using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.CountdownServiceModule.Data.ValueObjects;
using Modules.CountdownServiceModule.Models;

namespace Modules.CountdownServiceModule.Controllers
{
    /// <summary>
    /// Subscribes one caller's callbacks to a countdown that is already in the model. This runs
    /// both behind a fresh start and on its own, which is what lets several callers share one id.
    /// </summary>
    internal class AddCallbacksCommand : Command
    {
        [SignalParam] private CountdownRequestVO _request { get; set; }
        [Inject] private ICountdownModel _countdownModel { get; set; }

        public override void Execute()
        {
            Retain();

            lock (_countdownModel.LockObject)
            {
                if (!_countdownModel.DataMap.TryGetValue(_request.Id, out CountdownVO countdown))
                {
                    FlowLogger.LogError(FlowLogType.CountdownServiceModule,
                        $"{nameof(AddCallbacksCommand)} - no countdown is running with id '{_request.Id}'.");

                    // The sequence ends here rather than carrying on to report a first value for
                    // a countdown that does not exist.
                    Stop();
                    return;
                }

                if (_request.CountdownTick != null)
                {
                    if (_request.IsPercentageTick)
                        countdown.TickPercentageCallbacks.Add(_request.CountdownTick);
                    else
                        countdown.TickCallbacks.Add(_request.CountdownTick);
                }

                if (_request.ElapsedTimeTick != null)
                    countdown.TickElapsedTimeCallbacks.Add(_request.ElapsedTimeTick);

                if (_request.CountdownComplete != null)
                    countdown.CompleteCallbacks.Add(_request.CountdownComplete);

                if (_request.CountdownStop != null)
                    countdown.StopCallbacks.Add(_request.CountdownStop);

                if (_request.CheckActive != null)
                    countdown.CheckActiveCallbacks.Add(_request.CheckActive);
            }

            Release();
        }
    }
}
