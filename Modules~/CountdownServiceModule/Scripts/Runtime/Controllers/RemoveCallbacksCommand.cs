using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.CountdownServiceModule.Data.ValueObjects;
using Modules.CountdownServiceModule.Models;

namespace Modules.CountdownServiceModule.Controllers
{
    /// <summary>
    /// Unsubscribes one caller from a countdown, and drops the countdown itself once nothing is
    /// left listening to it - so a screen that unsubscribes as it closes leaves nothing running.
    /// </summary>
    internal class RemoveCallbacksCommand : Command
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
                        $"{nameof(RemoveCallbacksCommand)} - no countdown is running with id '{_request.Id}'.");

                    Release();
                    return;
                }

                if (_request.CountdownTick != null)
                {
                    // Which list it went into depended on a flag the caller passed when it
                    // subscribed. Removing from both spares the caller from having to remember.
                    countdown.TickCallbacks.Remove(_request.CountdownTick);
                    countdown.TickPercentageCallbacks.Remove(_request.CountdownTick);
                }

                if (_request.ElapsedTimeTick != null)
                    countdown.TickElapsedTimeCallbacks.Remove(_request.ElapsedTimeTick);

                if (_request.CountdownComplete != null)
                    countdown.CompleteCallbacks.Remove(_request.CountdownComplete);

                if (_request.CountdownStop != null)
                    countdown.StopCallbacks.Remove(_request.CountdownStop);

                if (countdown.HasNoListeners)
                {
                    _countdownModel.DataMap.Remove(_request.Id);
                    countdown.Clear();
                }
            }

            Release();
        }
    }
}
