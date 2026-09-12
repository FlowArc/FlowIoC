using FlowIoC.BaseModule.Connectors;
using FlowIoC.BaseModule.Contexts;
using Modules.GameplayModule.GameplayScreenModule.Signals;
using Modules.LoadingModule.LoadingScreenModule.Signals;
using Modules.MainModule.MainScreenModule.Signals;
using Modules.MainModule.Signals;

namespace Modules.ConnectorModule.RootsContexts
{
    public class MainConnectorSubContext : Context
    {
        private MainSignals _mainSignals;
        private MainScreenSignals _mainScreenSignals;
        private GameplayScreenSignals _gameplayScreenSignals;
        private LoadingScreenSignals _loadingScreenSignals;

        public override void Setup()
        {
            base.Setup();

            _mainSignals = InjectionBinderCrossContext.GetInstance<MainSignals>();
            _mainScreenSignals = InjectionBinderCrossContext.GetInstance<MainScreenSignals>();
            _gameplayScreenSignals = InjectionBinderCrossContext.GetInstance<GameplayScreenSignals>();
            _loadingScreenSignals = InjectionBinderCrossContext.GetInstance<LoadingScreenSignals>();

            IncomingSignals();
            OutgoingSignals();
        }

        private void IncomingSignals()
        {
            _mainSignals.Outgoing.Started.Connect(_mainScreenSignals.Incoming.OpenMainScreen);

            // The loading screen's retry button asks Main to boot again; the set it names is Main's boot.
            _loadingScreenSignals.Outgoing.RetryClicked.Connect(_ => _mainSignals.Incoming.RetryBoot.Dispatch());
        }

        private void OutgoingSignals()
        {
            _mainScreenSignals.Outgoing.DifficultySelected.Connect(_gameplayScreenSignals.Incoming.OpenGameplayScreen);
        }

        public override void DestroyContext()
        {
            UnbindIncomingSignals();
            UnbindOutgoingSignals();

            base.DestroyContext();
        }

        private void UnbindIncomingSignals()
        {
            _mainSignals.Outgoing.Started.Disconnect();
            _loadingScreenSignals.Outgoing.RetryClicked.Disconnect();
        }

        private void UnbindOutgoingSignals() =>
            _mainScreenSignals.Outgoing.DifficultySelected.Disconnect();
    }
}