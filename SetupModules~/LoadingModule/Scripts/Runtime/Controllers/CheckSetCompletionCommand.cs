using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.LoadingModule.Data.ValueObjects;
using Modules.LoadingModule.Enums;
using Modules.LoadingModule.Models;
using Modules.LoadingModule.Shared.Enums;
using Modules.LoadingModule.Signals;
using UnityEngine;

namespace Modules.LoadingModule.Controllers
{
    /// <summary>
    /// Ends a running set once every configured step has ended, announces which way it went, and
    /// reports the end into every step that stands for this set - which is how a child set's
    /// completion climbs into its parent's bar.
    /// </summary>
    internal class CheckSetCompletionCommand : Command
    {
        [Inject] private ILoadingModel _model { get; set; }
        [InjectSignal] private LoadingSignals _signals { get; set; }
        [InjectSignal] private LoadingInternalSignals _internalSignals { get; set; }

        [SignalParam] private string _set { get; set; }

        public override void Execute()
        {
            if (!_model.TryGetSet(_set, out LoadingSetRVO set)) return;
            if (set.State != LoadingSetState.Running) return;
            if (!_model.IsEnded(set)) return;

            float now = Time.realtimeSinceStartup;
            _model.End(set, now);

            if (set.State == LoadingSetState.Completed)
            {
                FlowLogger.Log(FlowLogType.LoadingModule,
                    $"CheckSetCompletionCommand - '{_set}' completed in {set.EndedAt - set.BeganAt:0.00} s.");
                _signals.Outgoing.SetCompleted.Dispatch(_set);
            }
            else
            {
                FlowLogger.Log(FlowLogType.LoadingModule,
                    $"CheckSetCompletionCommand - '{_set}' failed at '{set.FailedStep}' after {set.EndedAt - set.BeganAt:0.00} s.");
                _signals.Outgoing.SetFailed.Dispatch(_set, set.FailedStep);
            }

            foreach (LoadingStepRVO parent in _model.ParentStepsOf(_set))
            {
                if (parent.Owner.State != LoadingSetState.Running || parent.IsEnded) continue;

                _internalSignals.StepReported.Dispatch(new LoadingStepReportVO
                {
                    Step = parent.Config.Key,
                    Kind = set.State == LoadingSetState.Completed ? LoadingReportKind.Complete : LoadingReportKind.Fail,
                    Reason = set.State == LoadingSetState.Completed ? null : $"the set '{_set}' failed at '{set.FailedStep}'",
                    Time = now
                });
            }
        }
    }
}
