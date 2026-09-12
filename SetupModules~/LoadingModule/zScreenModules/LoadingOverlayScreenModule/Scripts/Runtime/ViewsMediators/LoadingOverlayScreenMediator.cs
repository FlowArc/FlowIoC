using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.Extensions;
using Modules.LoadingModule.LoadingOverlayScreenModule.Signals;
using Modules.LoadingModule.Shared.Data.ValueObjects;

namespace Modules.LoadingModule.LoadingOverlayScreenModule.ViewsMediators
{
    /// <summary>Applies the snapshots for the set on show and closes when that set ends. Nothing here decides.</summary>
    public class LoadingOverlayScreenMediator : IMediator
    {
        [Inject] private LoadingOverlayScreenView _view { get; set; }
        [InjectSignal] private LoadingOverlayScreenSignals _signals { get; set; }

        public void OnRegister()
        {
            _signals.Incoming.Apply.AddListener(OnApply);
            _signals.Incoming.Close.AddListener(OnClose);
        }

        public void OnRemove()
        {
            _signals.Incoming.Apply.RemoveListener(OnApply);
            _signals.Incoming.Close.RemoveListener(OnClose);
        }

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
    }
}