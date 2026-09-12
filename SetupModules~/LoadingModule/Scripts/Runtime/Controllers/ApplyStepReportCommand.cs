using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.LoadingModule.Data.ValueObjects;
using Modules.LoadingModule.Enums;
using Modules.LoadingModule.Models;
using Modules.LoadingModule.Shared.Enums;
using Modules.LoadingModule.Signals;

namespace Modules.LoadingModule.Controllers
{
    /// <summary>
    /// Reads one report into the model. A report into a set that is not running begins the set -
    /// quietly for a Silent set, with a warning for a visible one, because its screen was meant to
    /// be up before the first step started. The failed step of a failed set reporting again reopens
    /// the set instead. The step's set is touched, and so is every set whose step stands for it, so
    /// a parent's bar moves with its child.
    /// </summary>
    internal class ApplyStepReportCommand : Command
    {
        [Inject] private ILoadingModel _model { get; set; }
        [InjectSignal] private LoadingInternalSignals _internalSignals { get; set; }

        [SignalParam] private LoadingStepReportVO _report { get; set; }

        public override void Execute()
        {
            if (!_model.TryGetStep(_report.Step, out LoadingStepRVO step))
            {
                FlowLogger.LogError(FlowLogType.LoadingModule,
                    $"ApplyStepReportCommand - CD_LoadingSets lists no step named '{_report.Step}'.", _model.Config);
                return;
            }

            LoadingSetRVO set = step.Owner;

            if (set.State == LoadingSetState.Failed && step.Config.Key == set.FailedStep)
            {
                // The failed step trying again: the set runs on from where it stopped, the steps
                // that succeeded keep their end, and the screen stays as it is.
                FlowLogger.Log(FlowLogType.LoadingModule, $"ApplyStepReportCommand - '{set.Key}' reopened by '{_report.Step}'.");
                _model.Reopen(set, _report.Time);
                _internalSignals.WatchSet.Dispatch(set.Key);
            }
            else if (set.State != LoadingSetState.Running)
            {
                if (set.Config.Presentation != LoadingPresentation.Silent)
                    FlowLogger.LogWarning(FlowLogType.LoadingModule,
                        $"ApplyStepReportCommand - step '{_report.Step}' reported into '{set.Key}' before anybody called Begin, so the set began now. Begin it first, so its screen is up before the first step starts.");

                _model.Begin(set, _report.Time);
                _internalSignals.WatchSet.Dispatch(set.Key);
            }
            else if (_report.Kind == LoadingReportKind.Start && step.IsEnded)
            {
                FlowLogger.LogWarning(FlowLogType.LoadingModule,
                    $"ApplyStepReportCommand - step '{_report.Step}' had already ended and is running again.");
            }

            _model.Apply(step, _report);
            LogLifecycle(step);

            _internalSignals.SetTouched.Dispatch(set.Key);

            foreach (LoadingStepRVO parent in _model.ParentStepsOf(set.Key))
                if (parent.Owner.State == LoadingSetState.Running)
                    _internalSignals.SetTouched.Dispatch(parent.Owner.Key);
        }

        private void LogLifecycle(LoadingStepRVO step)
        {
            switch (_report.Kind)
            {
                case LoadingReportKind.Start:
                    FlowLogger.Log(FlowLogType.LoadingModule, $"ApplyStepReportCommand - '{step.Owner.Key}'/'{step.Config.Key}' started.");
                    break;
                case LoadingReportKind.Complete:
                    FlowLogger.Log(FlowLogType.LoadingModule,
                        $"ApplyStepReportCommand - '{step.Owner.Key}'/'{step.Config.Key}' completed in {step.EndedAt - step.StartedAt:0.00} s.");
                    break;
                case LoadingReportKind.Skip:
                    FlowLogger.Log(FlowLogType.LoadingModule, $"ApplyStepReportCommand - '{step.Owner.Key}'/'{step.Config.Key}' skipped.");
                    break;
                case LoadingReportKind.Fail:
                    FlowLogger.LogError(FlowLogType.LoadingModule,
                        $"'{step.Owner.Key}'/'{step.Config.Key}' failed{(string.IsNullOrEmpty(_report.Reason) ? "." : ": " + _report.Reason)}");
                    break;
            }
        }
    }
}
