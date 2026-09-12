using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.LoadingModule.LoadingOverlayScreenModule.Models;
using Modules.LoadingModule.Shared.Data.ValueObjects;

namespace Modules.LoadingModule.LoadingOverlayScreenModule.Controllers
{
    /// <summary>
    /// Every snapshot is remembered, so an overlay that finishes loading after the news arrived is
    /// filled from it. The Mediator applies the same signal to an overlay that is already up.
    /// </summary>
    [HideCommandLog]
    internal class RememberOverlayStatusCommand : Command
    {
        [Inject] private ILoadingOverlayScreenModel _model { get; set; }
        [SignalParam] private LoadingSetStatusRVO _status { get; set; }

        public override void Execute() => _model.Remember(_status);
    }
}
