using Modules.LoadingModule.Data.ValueObjects;
using Modules.LoadingModule.Enums;
using UnityEngine;

namespace Modules.LoadingModule.Services
{
    /// <summary>One step's reporting handle; each call is one report to the service.</summary>
    internal sealed class LoadingStep : ILoadingStep
    {
        private readonly LoadingService _service;
        private readonly string _step;

        internal LoadingStep(LoadingService service, string step)
        {
            _service = service;
            _step = step;
        }

        [HideInCallstack]
        public void Start() => _service.Submit(new LoadingStepReportVO {Step = _step, Kind = LoadingReportKind.Start});

        [HideInCallstack]
        public void Progress(float fraction) =>
            _service.Submit(new LoadingStepReportVO {Step = _step, Kind = LoadingReportKind.Progress, Fraction = fraction});

        [HideInCallstack]
        public void Detail(string text) =>
            _service.Submit(new LoadingStepReportVO {Step = _step, Kind = LoadingReportKind.Detail, Detail = text});

        [HideInCallstack]
        public void Complete() => _service.Submit(new LoadingStepReportVO {Step = _step, Kind = LoadingReportKind.Complete});

        [HideInCallstack]
        public void Skip() => _service.Submit(new LoadingStepReportVO {Step = _step, Kind = LoadingReportKind.Skip});

        [HideInCallstack]
        public void Fail(string reason = null) =>
            _service.Submit(new LoadingStepReportVO {Step = _step, Kind = LoadingReportKind.Fail, Reason = reason, Detail = reason});
    }
}
