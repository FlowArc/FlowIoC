using System.Collections;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.Provider.Coroutine;
using FlowIoC.ConsoleModule;
using Modules.LoadingModule.Data.ValueObjects;
using Modules.LoadingModule.Models;
using Modules.LoadingModule.Shared.Enums;
using UnityEngine;

namespace Modules.LoadingModule.Controllers
{
    /// <summary>
    /// Watches one running set and says so, once per silence, when nothing has reported for the
    /// set's StallWarningSeconds. It does not retain: the watch outlives the step, on the coroutine
    /// provider, and ends by itself when the set ends. Captured locals only - the Command instance
    /// goes back to the pool the moment Execute returns.
    /// </summary>
    internal class WatchStallCommand : Command
    {
        private const float CHECK_EVERY_SECONDS = 0.5f;

        [Inject] private ILoadingModel _model { get; set; }
        [Inject] private ICoroutineProvider _coroutineProvider { get; set; }

        [SignalParam] private string _set { get; set; }

        public override void Execute()
        {
            if (!_model.TryGetSet(_set, out LoadingSetRVO set)) return;
            if (set.WatcherRunning) return;

            set.WatcherRunning = true;
            _coroutineProvider.StartCoroutine(Watch(_model, set));
        }

        private static IEnumerator Watch(ILoadingModel model, LoadingSetRVO set)
        {
            var wait = new WaitForSecondsRealtime(CHECK_EVERY_SECONDS);

            while (set.State == LoadingSetState.Running)
            {
                if (model.TryTakeStallWarning(set, Time.realtimeSinceStartup, out string warning))
                    FlowLogger.LogWarning(FlowLogType.LoadingModule, warning);

                yield return wait;
            }

            set.WatcherRunning = false;
        }
    }
}
