#if UNITY_EDITOR

using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using Modules.AnalyticsModule.AnalyticsTestModule.Signals;

namespace Modules.AnalyticsModule.AnalyticsTestModule.ViewsMediators
{
    public class AnalyticsTestMediator : IMediator
    {
        [Inject] private AnalyticsTestView _view { get; set; }

        [InjectSignal] private AnalyticsTestInternalSignals _signals { get; set; }

        public void OnRegister()
        {
            _view.OnLogTestEvent += LogTestEvent;
            _view.OnLogStep += LogStep;
            _view.OnSetProperty += SetProperty;
            _view.OnSetId += SetId;
            _view.OnConsentGranted += ConsentGranted;
            _view.OnConsentDenied += ConsentDenied;
            _view.OnAnswerProvider += AnswerProvider;
            _signals.StateChanged.AddListener(OnStateChanged);

            // The view may register after the Roots have launched, so the first report is asked for
            // here as well as from Launch, or the label could keep its placeholder.
            _signals.ReportState.Dispatch();
        }

        public void OnRemove()
        {
            _view.OnLogTestEvent -= LogTestEvent;
            _view.OnLogStep -= LogStep;
            _view.OnSetProperty -= SetProperty;
            _view.OnSetId -= SetId;
            _view.OnConsentGranted -= ConsentGranted;
            _view.OnConsentDenied -= ConsentDenied;
            _view.OnAnswerProvider -= AnswerProvider;
            _signals.StateChanged.RemoveListener(OnStateChanged);
        }

        private void LogTestEvent() => _signals.LogTestEvent.Dispatch();
        private void LogStep() => _signals.LogStep.Dispatch();
        private void SetProperty() => _signals.SetProperty.Dispatch();
        private void SetId() => _signals.SetId.Dispatch();
        private void ConsentGranted() => _signals.ConsentGranted.Dispatch();
        private void ConsentDenied() => _signals.ConsentDenied.Dispatch();
        private void AnswerProvider(bool ready) => _signals.AnswerProvider.Dispatch(ready);

        private void OnStateChanged(string text) => _view.SetStatus(text);
    }
}

#endif
