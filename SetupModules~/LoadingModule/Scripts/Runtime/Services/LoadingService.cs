using System.Threading.Tasks;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.LoadingModule.Data.ValueObjects;
using Modules.LoadingModule.Models;
using Modules.LoadingModule.Shared.Enums;
using Modules.LoadingModule.Signals;
using UnityEngine;

namespace Modules.LoadingModule.Services
{
    /// <summary>
    /// The module's public surface. It validates what it is asked and hands the work to the
    /// Commands through the internal signals, so every change to a set is a step the Flow Console
    /// can show. Every entry point hides itself from the call stack: an error raised here opens
    /// the Command that reported, not this guard.
    /// </summary>
    public class LoadingService : ILoadingService
    {
        [Inject] private ILoadingModel _model { get; set; }
        [InjectSignal] private LoadingInternalSignals _internalSignals { get; set; }

        [HideInCallstack]
        public void Begin(string set)
        {
            if (!_model.TryGetSet(set, out _))
            {
                FlowLogger.LogError(FlowLogType.LoadingModule,
                    $"Begin('{set}') - CD_LoadingSets declares no set named '{set}'.");
                return;
            }

            _internalSignals.BeginSet.Dispatch(set);
        }

        [HideInCallstack]
        public ILoadingStep Report(string step) => new LoadingStep(this, step);

        [HideInCallstack]
        public Task<bool> Await(string set)
        {
            if (!_model.TryGetSet(set, out LoadingSetRVO runtime))
            {
                FlowLogger.LogError(FlowLogType.LoadingModule,
                    $"Await('{set}') - CD_LoadingSets declares no set named '{set}'.");
                return Task.FromResult(false);
            }

            switch (runtime.State)
            {
                case LoadingSetState.Completed: return Task.FromResult(true);
                case LoadingSetState.Failed: return Task.FromResult(false);
                default: return _model.Awaiter(runtime).Task;
            }
        }

        [HideInCallstack]
        internal void Submit(LoadingStepReportVO report)
        {
            if (!_model.TryGetStep(report.Step, out _))
            {
                FlowLogger.LogError(FlowLogType.LoadingModule,
                    $"Report('{report.Step}') - CD_LoadingSets lists no step named '{report.Step}' in any set.");
                return;
            }

            report.Time = Time.realtimeSinceStartup;
            _internalSignals.StepReported.Dispatch(report);
        }
    }
}
