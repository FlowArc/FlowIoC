using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.LoadingModule.LoadingOverlayScreenModule.Models;

namespace Modules.LoadingModule.LoadingOverlayScreenModule.Controllers
{
    internal class RememberOverlayClosedCommand : Command
    {
        [Inject] private ILoadingOverlayScreenModel _model { get; set; }
        [SignalParam] private string _set { get; set; }

        public override void Execute() => _model.RememberClosed(_set);
    }
}
