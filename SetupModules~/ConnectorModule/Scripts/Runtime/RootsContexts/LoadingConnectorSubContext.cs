using FlowIoC.BaseModule.Connectors;
using FlowIoC.BaseModule.Contexts;
using Modules.LoadingModule.LoadingOverlayScreenModule.Signals;
using Modules.LoadingModule.LoadingScreenModule.Signals;
using Modules.LoadingModule.Signals;

namespace Modules.ConnectorModule.RootsContexts
{
    /// <summary>
    /// The loading service and its two screens meet here. One signal per presentation is what lets
    /// each Began reach its own screen without an if; SetChanged, SetCompleted and SetFailed reach
    /// both, and each screen's Mediator applies only what concerns the set it is showing.
    /// </summary>
    public class LoadingConnectorSubContext : Context
    {
        private const string GROUP = nameof(LoadingConnectorSubContext);

        private LoadingSignals _loadingSignals;
        private LoadingScreenSignals _loadingScreenSignals;
        private LoadingOverlayScreenSignals _loadingOverlayScreenSignals;

        public override void Setup()
        {
            base.Setup();

            _loadingSignals = InjectionBinderCrossContext.GetInstance<LoadingSignals>();
            _loadingScreenSignals = InjectionBinderCrossContext.GetInstance<LoadingScreenSignals>();
            _loadingOverlayScreenSignals = InjectionBinderCrossContext.GetInstance<LoadingOverlayScreenSignals>();

            IncomingSignals();
        }

        private void IncomingSignals()
        {
            _loadingSignals.Outgoing.FullscreenBegan.Connect(_loadingScreenSignals.Incoming.Open, GROUP);
            _loadingSignals.Outgoing.SetChanged.Connect(_loadingScreenSignals.Incoming.Apply, GROUP);
            _loadingSignals.Outgoing.SetCompleted.Connect(_loadingScreenSignals.Incoming.Close, GROUP);
            _loadingSignals.Outgoing.SetFailed.Connect(_loadingScreenSignals.Incoming.ShowFailed, GROUP);

            _loadingSignals.Outgoing.OverlayBegan.Connect(_loadingOverlayScreenSignals.Incoming.Open, GROUP);
            _loadingSignals.Outgoing.SetChanged.Connect(_loadingOverlayScreenSignals.Incoming.Apply, GROUP);
            _loadingSignals.Outgoing.SetCompleted.Connect(_loadingOverlayScreenSignals.Incoming.Close, GROUP);
            _loadingSignals.Outgoing.SetFailed.Connect(_loadingOverlayScreenSignals.Incoming.Close, (set, step) => set, GROUP);
        }

        public override void DestroyContext()
        {
            SignalConnector.DisconnectGroup(GROUP);
            base.DestroyContext();
        }
    }
}
