using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.LoadingModule.LoadingScreenModule.Models;

namespace Modules.LoadingModule.LoadingScreenModule.Controllers
{
    internal class RememberFailedCommand : Command
    {
        [Inject] private ILoadingScreenModel _model { get; set; }
        [SignalParam(0)] private string _set { get; set; }
        [SignalParam(1)] private string _step { get; set; }

        public override void Execute() => _model.RememberFailed(_set, _step);
    }
}
