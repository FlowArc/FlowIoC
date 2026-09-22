using FlowIoC.BaseModule.Connectors;
using FlowIoC.BaseModule.Contexts;
using Modules.BotBarModule.BotBarScreenModule.Signals;
using Modules.BotBarModule.Signals;

namespace Modules.BotBarModule.BotBarConnectorModule.RootsContexts
{
    /// <summary>
    /// The bar's System and its screen meet here, the way a service and its screens meet in the
    /// setup set's ConnectorModule. It ships with the module because the screen may not reference
    /// its parent's Signals assembly, and the crossing is the module's own business rather than
    /// the game's. Named after the counterpart, the screen.
    /// </summary>
    public class BotBarScreenConnectorSubContext : Context
    {
        private const string GROUP = nameof(BotBarScreenConnectorSubContext);

        private BotBarSignals _botBarSignals;
        private BotBarScreenSignals _screenSignals;

        public override void Setup()
        {
            base.Setup();

            _botBarSignals = InjectionBinderCrossContext.GetInstance<BotBarSignals>();
            _screenSignals = InjectionBinderCrossContext.GetInstance<BotBarScreenSignals>();

            IncomingSignals();
            OutgoingSignals();
        }

        /// <summary>What the System announces, applied by the screen.</summary>
        private void IncomingSignals()
        {
            _botBarSignals.Outgoing.Opened.Connect(_screenSignals.Incoming.Open, GROUP);
            _botBarSignals.Outgoing.SelectionChanged.Connect(_screenSignals.Incoming.ApplySelection, GROUP);
            _botBarSignals.Outgoing.TabStateChanged.Connect(_screenSignals.Incoming.ApplyTabState, GROUP);
            _botBarSignals.Outgoing.BadgeChanged.Connect(_screenSignals.Incoming.ApplyBadge, GROUP);
            _botBarSignals.Outgoing.Shown.Connect(_screenSignals.Incoming.Show, GROUP);
            _botBarSignals.Outgoing.Hidden.Connect(_screenSignals.Incoming.Hide, GROUP);
        }

        /// <summary>What the screen reports, decided by the System.</summary>
        private void OutgoingSignals()
        {
            _screenSignals.Outgoing.TabTapped.Connect(_botBarSignals.Incoming.SelectTab, GROUP);
        }

        public override void DestroyContext()
        {
            SignalConnector.DisconnectGroup(GROUP);
            base.DestroyContext();
        }
    }
}
