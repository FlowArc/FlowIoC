using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AnalyticsModule.Models;
using Modules.AnalyticsModule.Services;

namespace Modules.AnalyticsModule.Controllers
{
    /// <summary>The slot goes; whatever it still queued goes with it. A provider that was never plugged is a plain log, because teardown order is not the caller's to control.</summary>
    internal class UnplugProviderCommand : Command
    {
        [Inject] private IAnalyticsModel _model { get; set; }

        [SignalParam] private IAnalyticsProvider _provider { get; set; }

        public override void Execute()
        {
            if (_provider == null)
                return;

            if (!_model.Unplug(_provider))
            {
                FlowLogger.Log($"Unplug - '{_provider.Name}' was not plugged.");
                return;
            }

            FlowLogger.Log($"Unplug - {_provider.Name}");
        }
    }
}
