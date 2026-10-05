using FlowIoC.BaseModule.Connectors;
using FlowIoC.BaseModule.Contexts;
using Modules.LoadingModule.LoadingScreenModule.Signals;
using Modules.LoadingModule.Signals;

namespace Modules.ConnectorModule.RootsContexts
{
    /// <summary>
    /// The loading service and its screen meet here. FullscreenBegan opens the screen; SetChanged,
    /// SetCompleted and SetFailed reach it for every set, and its Mediator applies only what
    /// concerns the set it is showing.
    /// </summary>
    public class LoadingConnectorSubContext : Context
    {
        private const string GROUP = nameof(LoadingConnectorSubContext);

        private LoadingSignals _loadingSignals;
        private LoadingScreenSignals _loadingScreenSignals;

        public override void Setup()
        {
            base.Setup();

            _loadingSignals = InjectionBinderCrossContext.GetInstance<LoadingSignals>();
            _loadingScreenSignals = InjectionBinderCrossContext.GetInstance<LoadingScreenSignals>();

            IncomingSignals();
        }

        private void IncomingSignals()
        {
            _loadingSignals.Outgoing.FullscreenBegan.Connect(_loadingScreenSignals.Incoming.Open, GROUP);
            _loadingSignals.Outgoing.SetChanged.Connect(_loadingScreenSignals.Incoming.Apply, GROUP);
            _loadingSignals.Outgoing.SetCompleted.Connect(_loadingScreenSignals.Incoming.Close, GROUP);
            _loadingSignals.Outgoing.SetFailed.Connect(_loadingScreenSignals.Incoming.ShowFailed, GROUP);
        }

        public override void DestroyContext()
        {
            SignalConnector.DisconnectGroup(GROUP);
            base.DestroyContext();
        }
    }
}
