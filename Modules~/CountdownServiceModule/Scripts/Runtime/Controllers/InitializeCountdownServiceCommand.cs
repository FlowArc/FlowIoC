using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.CountdownServiceModule.Data.ValueObjects;
using Modules.CountdownServiceModule.Models;
using Modules.CountdownServiceModule.Services;
using Modules.CountdownServiceModule.Signals;
using UnityEngine;

namespace Modules.CountdownServiceModule.Controllers
{
    /// <summary>
    /// Asks the time source to make itself ready and brings the module up once it has.
    /// Countdowns requested before this point are already waiting in the model, so activating
    /// also tells each of them that the service is running.
    /// </summary>
    internal class InitializeCountdownServiceCommand : Command
    {
        [Inject] private ICountdownModel _countdownModel { get; set; }
        [Inject] private ITimeSource _timeSource { get; set; }
        [InjectSignal] private CountdownServiceSignals _signals { get; set; }

        public override void Execute()
        {
            if (_countdownModel.IsActive)
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
                FlowLogger.LogError(FlowLogType.CountdownServiceModule,
                    $"{nameof(InitializeCountdownServiceCommand)} - the time source could not be prepared. "
                    + "Nothing will tick until Initialize is dispatched again and succeeds.");

                _signals.Outgoing.Ready.Dispatch(false);
                Stop();
                return;
            }

            _countdownModel.Activate(_timeSource.UtcNow, Time.realtimeSinceStartup);

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
            lock (_countdownModel.LockObject)
            {
                foreach (CountdownVO countdown in _countdownModel.DataMap.Values)
                {
                    foreach (var checkActive in countdown.CheckActiveCallbacks.ToArray())
                        checkActive.Invoke(true);
                }
            }
        }
    }
}
