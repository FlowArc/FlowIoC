using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.LoadingModule.LoadingScreenModule.Models;
using Modules.LoadingModule.Shared.Data.ValueObjects;

namespace Modules.LoadingModule.LoadingScreenModule.Controllers
{
    /// <summary>
    /// Every snapshot is remembered, so a screen that finishes loading after the news arrived is
    /// filled from it. The Mediator applies the same signal to a screen that is already up.
    /// </summary>
    [HideCommandLog]
    internal class RememberStatusCommand : Command
    {
        [Inject] private ILoadingScreenModel _model { get; set; }
        [SignalParam] private LoadingSetStatusRVO _status { get; set; }

        public override void Execute() => _model.Remember(_status);
    }
}
