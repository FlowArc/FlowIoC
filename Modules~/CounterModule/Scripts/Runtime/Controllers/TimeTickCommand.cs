using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.Provider.Coroutine;
using Modules.CounterModule.Models;
using UnityEngine;

namespace Modules.CounterModule.Controllers
{
    /// <summary>
    /// Waits a second, then moves the module's clock. The new time is derived from the moment
    /// the module started rather than added to the previous reading, so a frame that took longer
    /// than a second does not leave every counter running slow.
    /// </summary>
    [HideCommandLog]
    internal class TimeTickCommand : Command
    {
        [Inject] private ICoroutineProvider _coroutineProvider { get; set; }
        [Inject] private ICounterModel _counterModel { get; set; }

        public override void Execute()
        {
            Retain();

            _coroutineProvider.WaitForSecondsRealTime(1, OnSecondElapsed);
        }

        private void OnSecondElapsed()
        {
            _counterModel.Time = _counterModel.StartTime
                .AddSeconds(Time.realtimeSinceStartup - _counterModel.StartRealtime);

            Release();
        }
    }
}
