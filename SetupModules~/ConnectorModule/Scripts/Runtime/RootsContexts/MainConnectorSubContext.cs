using FlowIoC.BaseModule.Connectors;
using FlowIoC.BaseModule.Contexts;
using Modules.GameplayModule.GameplayScreenModule.Shared.Signals;
using Modules.MainModule.MainScreenModule.Shared.Signals;
using Modules.MainModule.Shared.Signals;

namespace Modules.ConnectorModule.RootsContexts
{
    public class MainConnectorSubContext : Context
    {
        private MainSignals _mainSignals;
        private MainScreenSignals _mainScreenSignals;
        private GameplayScreenSignals _gameplayScreenSignals;

        public override void Setup()
        {
            base.Setup();

            _mainSignals = InjectionBinderCrossContext.GetInstance<MainSignals>();
            _mainScreenSignals = InjectionBinderCrossContext.GetInstance<MainScreenSignals>();
            _gameplayScreenSignals = InjectionBinderCrossContext.GetInstance<GameplayScreenSignals>();

            IncomingSignals();
            OutGoingSignals();
        }

        private void IncomingSignals()
        {
            _mainSignals.Outgoing.OpenMainScene.Connect(_mainScreenSignals.Incoming.OpenMainScreen);
        }

        private void OutGoingSignals()
        {
            _mainScreenSignals.Outgoing.DifficultySelected.Connect(_gameplayScreenSignals.Incoming.OpenGameplayScreen);
        }

        public override void DestroyContext()
        {
            UnbindIncomingSignals();
            UnbindOutGoingSignals();

            base.DestroyContext();
        }

        private void UnbindIncomingSignals() =>
            _mainSignals.Outgoing.OpenMainScene.Disconnect();

        private void UnbindOutGoingSignals() =>
            _mainScreenSignals.Outgoing.DifficultySelected.Disconnect();
    }
}