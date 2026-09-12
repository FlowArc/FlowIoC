using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.Extensions;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using Modules.LoadingModule.LoadingScreenModule.Signals;
using Modules.LoadingModule.Shared.Data.ValueObjects;

namespace Modules.LoadingModule.LoadingScreenModule.ViewsMediators
{
    /// <summary>
    /// Applies the snapshots that arrive for the set on show, closes on its completion, shows the
    /// failed state on its failure. Nothing here decides: a retry is announced and whoever owns
    /// the set decides what a retry is.
    /// </summary>
    public class LoadingScreenMediator : IMediator
    {
        [Inject] private LoadingScreenView _view { get; set; }
        [InjectSignal] private LoadingScreenSignals _signals { get; set; }

        public void OnRegister()
        {
            _view.ShowCompleted += OnScreenShown;
            _view.HideCompleted += OnScreenHidden;

            _signals.Incoming.Apply.AddListener(OnApply);
            _signals.Incoming.Close.AddListener(OnClose);
            _signals.Incoming.ShowFailed.AddListener(OnShowFailed);
        }

        public void OnRemove()
        {
            _view.ShowCompleted -= OnScreenShown;
            _view.HideCompleted -= OnScreenHidden;

            _signals.Incoming.Apply.RemoveListener(OnApply);
            _signals.Incoming.Close.RemoveListener(OnClose);
            _signals.Incoming.ShowFailed.RemoveListener(OnShowFailed);
        }

        private void OnScreenShown(IScreenBody screen) => _view.Retry += OnRetry;

        private void OnScreenHidden(IScreenBody screen) => _view.Retry -= OnRetry;

        private bool IsShowing(string set) =>
            _view.Data.HasState(ScreenState.AvailableToSendSignal) && _view.ShowingSet == set;

        private void OnApply(LoadingSetStatusRVO status)
        {
            if (!IsShowing(status.Set)) return;
            _view.Apply(status);
        }

        private void OnClose(string set)
        {
            if (!IsShowing(set)) return;
            _view.Hide();
        }

        private void OnShowFailed(string set, string step)
        {
            if (!IsShowing(set)) return;
            _view.ShowFailed(step);
        }

        private void OnRetry()
        {
            if (!_view.Data.HasState(ScreenState.AvailableToSendSignal) || _view.ShowingSet == null) return;

            _view.HideFailed();
            _signals.Outgoing.RetryClicked.Dispatch(_view.ShowingSet);
        }
    }
}