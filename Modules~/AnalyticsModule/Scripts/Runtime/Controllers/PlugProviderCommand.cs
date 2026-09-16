using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AnalyticsModule.Models;
using Modules.AnalyticsModule.Services;

namespace Modules.AnalyticsModule.Controllers
{
    /// <summary>A provider into a slot of its own. Plugged once: a second plug of the same instance is the caller's mistake and is reported.</summary>
    internal class PlugProviderCommand : Command
    {
        [Inject] private IAnalyticsModel _model { get; set; }

        [SignalParam] private IAnalyticsProvider _provider { get; set; }

        public override void Execute()
        {
            if (_provider == null)
            {
                FlowLogger.LogError("Plug - a null provider was handed in; nothing plugged.");
                return;
            }

            if (_model.TryGetSlot(_provider, out _))
            {
                FlowLogger.LogError($"Plug - '{_provider.Name}' is already plugged; a provider is plugged once.");
                return;
            }

            _model.Plug(_provider);
            FlowLogger.Log($"Plug - {_provider.Name}");
        }
    }
}
