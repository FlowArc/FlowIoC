#if UNITY_EDITOR

using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using Modules.MobileNotificationModule.MobileNotificationTestModule.Signals;

namespace Modules.MobileNotificationModule.MobileNotificationTestModule.ViewsMediators
{
    public class MobileNotificationTestMediator : IMediator
    {
        [Inject] private MobileNotificationTestView _view { get; set; }

        [InjectSignal] private MobileNotificationTestInternalSignals _signals { get; set; }

        public void OnRegister()
        {
            _view.OnRequestPermission += RequestPermission;
            _view.OnSchedule += Schedule;
            _view.OnCancel += Cancel;
            _view.OnCancelAll += CancelAll;
            _view.OnOpenSettings += OpenSettings;
            _view.OnLeave += Leave;
            _view.OnReturn += Return;
            _signals.StateChanged.AddListener(OnStateChanged);

            // The view may register after the Roots have launched, so the first report is asked for
            // here as well as from Launch, or the label could keep its placeholder.
            _signals.ReportState.Dispatch();
        }

        public void OnRemove()
        {
            _view.OnRequestPermission -= RequestPermission;
            _view.OnSchedule -= Schedule;
            _view.OnCancel -= Cancel;
            _view.OnCancelAll -= CancelAll;
            _view.OnOpenSettings -= OpenSettings;
            _view.OnLeave -= Leave;
            _view.OnReturn -= Return;
            _signals.StateChanged.RemoveListener(OnStateChanged);
        }

        private void RequestPermission() => _signals.RequestPermission.Dispatch();
        private void Schedule(string tag) => _signals.ScheduleTest.Dispatch(tag);
        private void Cancel(string tag) => _signals.CancelTest.Dispatch(tag);
        private void CancelAll() => _signals.CancelAll.Dispatch();
        private void OpenSettings() => _signals.OpenSettings.Dispatch();
        private void Leave() => _signals.Leave.Dispatch();
        private void Return() => _signals.Return.Dispatch();

        private void OnStateChanged(string text) => _view.SetStatus(text);
    }
}

#endif