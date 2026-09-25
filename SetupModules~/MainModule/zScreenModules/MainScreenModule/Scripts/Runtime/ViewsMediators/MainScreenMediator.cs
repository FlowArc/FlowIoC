using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.Extensions;
using Modules.MainModule.MainScreenModule.Signals;

namespace Modules.MainModule.MainScreenModule.ViewsMediators
{
    public class MainScreenMediator : IMediator
    {
        [Inject] private MainScreenView _view { get; set; }
        [InjectSignal] private MainScreenSignals _signals { get; set; }

        public virtual void OnRegister()
        {
            _view.PlayClicked += OnPlayClicked;
        }

        public virtual void OnRemove()
        {
            _view.PlayClicked -= OnPlayClicked;
        }

        private void OnPlayClicked()
        {
            if (!_view.Data.HasState(ScreenState.AvailableToSendSignal))
                return;

            _view.Hide();
            _signals.Outgoing.PlayClicked.Dispatch();
        }
    }
}
