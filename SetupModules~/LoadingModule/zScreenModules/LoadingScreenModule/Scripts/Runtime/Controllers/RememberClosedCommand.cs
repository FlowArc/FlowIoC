using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.LoadingModule.LoadingScreenModule.Models;

namespace Modules.LoadingModule.LoadingScreenModule.Controllers
{
    internal class RememberClosedCommand : Command
    {
        [Inject] private ILoadingScreenModel _model { get; set; }
        [SignalParam] private string _set { get; set; }

        public override void Execute() => _model.RememberClosed(_set);
    }
}
