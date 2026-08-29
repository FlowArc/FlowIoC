using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.Provider.Coroutine;
using Modules.CountdownServiceModule.Models;
using UnityEngine;

namespace Modules.CountdownServiceModule.Controllers
{
    /// <summary>
    /// Waits a second, then moves the module's clock. The new time is derived from the moment
    /// the module started rather than added to the previous reading, so a frame that took longer
    /// than a second does not leave every countdown running slow.
    /// </summary>
    [HideCommandLog]
    internal class TimeTickCommand : Command
    {
        [Inject] private ICoroutineProvider _coroutineProvider { get; set; }
        [Inject] private ICountdownModel _countdownModel { get; set; }

        public override void Execute()
        {
            Retain();

            _coroutineProvider.WaitForSecondsRealTime(1, OnSecondElapsed);
        }

        private void OnSecondElapsed()
        {
            _countdownModel.Time = _countdownModel.StartTime
                .AddSeconds(Time.realtimeSinceStartup - _countdownModel.StartRealtime);

            Release();
        }
    }
}
