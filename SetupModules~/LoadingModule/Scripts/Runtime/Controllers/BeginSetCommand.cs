using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.LoadingModule.Data.ValueObjects;
using Modules.LoadingModule.Models;
using Modules.LoadingModule.Shared.Enums;
using Modules.LoadingModule.Signals;
using UnityEngine;

namespace Modules.LoadingModule.Controllers
{
    /// <summary>
    /// Starts a run of a set: every step back to pending, the stall watcher on, and the set touched
    /// so its Began and first snapshot go out. A set already running is left alone - a second Begin
    /// mid-run would throw away the reports that arrived.
    /// </summary>
    internal class BeginSetCommand : Command
    {
        [Inject] private ILoadingModel _model { get; set; }
        [InjectSignal] private LoadingInternalSignals _internalSignals { get; set; }

        [SignalParam] private string _set { get; set; }

        public override void Execute()
        {
            if (!_model.TryGetSet(_set, out LoadingSetRVO set))
            {
                FlowLogger.LogError(FlowLogType.LoadingModule,
                    $"BeginSetCommand - CD_LoadingSets declares no set named '{_set}'.", _model.Config);
                return;
            }

            if (set.State == LoadingSetState.Running)
            {
                FlowLogger.LogWarning(FlowLogType.LoadingModule,
                    $"BeginSetCommand - '{_set}' is already running; the second Begin is ignored.");
                return;
            }

            _model.Begin(set, Time.realtimeSinceStartup);
            FlowLogger.Log(FlowLogType.LoadingModule, $"BeginSetCommand - '{_set}' began ({set.Config.Presentation}).");

            _internalSignals.SetTouched.Dispatch(_set);
            _internalSignals.WatchSet.Dispatch(_set);
        }
    }
}
