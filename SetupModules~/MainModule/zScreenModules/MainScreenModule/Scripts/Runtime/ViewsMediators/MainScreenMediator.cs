using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.Extensions;
using Modules.GameplayModule.Shared.Enums;
using Modules.MainModule.MainScreenModule.Shared.Signals;

namespace Modules.MainModule.MainScreenModule.ViewsMediators
{
    public class MainScreenMediator : IMediator
    {
		[Inject] private MainScreenView _view { get; set; }
		[InjectSignal] private MainScreenSignals _signals { get; set; }

        public virtual void OnRegister()
        {
            _view.DifficultyClicked += OnDifficultyClicked;
        }

        public virtual void OnRemove()
        {
            _view.DifficultyClicked -= OnDifficultyClicked;
        }

        private void OnDifficultyClicked(DifficultyType difficulty)
        {
            if (!_view.Data.HasState(ScreenState.AvailableToSendSignal))
                return;

            _view.Hide();
            _signals.Outgoing.DifficultySelected.Dispatch(difficulty);
        }
    }
}
