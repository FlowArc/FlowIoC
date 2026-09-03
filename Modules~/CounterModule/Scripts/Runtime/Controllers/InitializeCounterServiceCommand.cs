using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.CounterModule.Data.ValueObjects;
using Modules.CounterModule.Models;
using Modules.CounterModule.Services;
using Modules.CounterModule.Shared.Signals;
using UnityEngine;

namespace Modules.CounterModule.Controllers
{
    /// <summary>
    /// Asks the time source to make itself ready and brings the module up once it has.
    /// Counters requested before this point are already waiting in the model, so activating
    /// also tells each of them that the service is running.
    /// </summary>
    internal class InitializeCounterServiceCommand : Command
    {
        [Inject] private ICounterModel _counterModel { get; set; }
        [Inject] private ITimeSource _timeSource { get; set; }
        [InjectSignal] private CounterServiceSignals _signals { get; set; }

        public override void Execute()
        {
            if (_counterModel.IsActive)
                return;

            Retain();

            _timeSource.Prepare(OnPrepared);
        }

        /// <summary>
        /// Runs as soon as the source answers, which for the device clock is before
        /// <see cref="Execute"/> has returned and for a server source is some time later.
        /// </summary>
        private void OnPrepared(bool isPrepared)
        {
            if (!isPrepared)
            {
                FlowLogger.LogError(FlowLogType.CounterModule,
                    $"{nameof(InitializeCounterServiceCommand)} - the time source could not be prepared. "
                    + "Nothing will tick until Initialize is dispatched again and succeeds.");

                _signals.Outgoing.Ready.Dispatch(false);
                Stop();
                return;
            }

            _counterModel.Activate(_timeSource.UtcNow, Time.realtimeSinceStartup);

            AnnounceActive();

            _signals.Outgoing.Ready.Dispatch(true);

            Release();
        }

        /// <summary>
        /// Everything asked for while the module was still coming up has been holding a
        /// checkActive callback that was told false. This is the second half of that promise.
        /// </summary>
        private void AnnounceActive()
        {
            lock (_counterModel.LockObject)
            {
                foreach (CounterVO counter in _counterModel.DataMap.Values)
                {
                    foreach (var checkActive in counter.CheckActiveCallbacks.ToArray())
                        checkActive.Invoke(true);
                }
            }
        }
    }
}
