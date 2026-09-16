using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AnalyticsModule.Models;

namespace Modules.AnalyticsModule.Controllers
{
    /// <summary>The one step that says the service has launched, so a plug before launch can be told from one after it.</summary>
    internal class MarkLaunchedCommand : Command
    {
        [Inject] private IAnalyticsModel _model { get; set; }

        public override void Execute() => _model.MarkLaunched();
    }
}
