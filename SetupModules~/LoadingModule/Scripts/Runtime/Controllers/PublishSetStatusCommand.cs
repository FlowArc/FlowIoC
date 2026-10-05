using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.LoadingModule.Data.ValueObjects;
using Modules.LoadingModule.Models;
using Modules.LoadingModule.Shared.Data.ValueObjects;
using Modules.LoadingModule.Shared.Enums;
using Modules.LoadingModule.Signals;

namespace Modules.LoadingModule.Controllers
{
    /// <summary>
    /// Publishes the set's snapshot. The first publish of a Fullscreen run announces it began, so
    /// the Connector can open the loading screen, and every publish announces the change.
    /// </summary>
    internal class PublishSetStatusCommand : Command
    {
        [Inject] private ILoadingModel _model { get; set; }
        [InjectSignal] private LoadingSignals _signals { get; set; }

        [SignalParam] private string _set { get; set; }

        public override void Execute()
        {
            if (!_model.TryGetSet(_set, out LoadingSetRVO set)) return;

            LoadingSetStatusRVO status = _model.BuildStatus(set);

            if (!set.BeginAnnounced && set.State == LoadingSetState.Running)
            {
                set.BeginAnnounced = true;

                if (set.Config.Presentation == LoadingPresentation.Fullscreen)
                    _signals.Outgoing.FullscreenBegan.Dispatch(status);
            }

            _signals.Outgoing.SetChanged.Dispatch(status);
        }
    }
}
