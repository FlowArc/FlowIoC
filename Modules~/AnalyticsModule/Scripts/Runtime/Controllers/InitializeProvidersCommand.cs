using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AnalyticsModule.Data.ValueObjects;
using Modules.AnalyticsModule.Enums;
using Modules.AnalyticsModule.Models;
using Modules.AnalyticsModule.Services;
using Modules.AnalyticsModule.Signals;

namespace Modules.AnalyticsModule.Controllers
{
    /// <summary>
    /// Every slot still Plugged is asked to initialize, once the service has launched. Before
    /// launch nothing happens: a provider plugged in Setup waits for Launch. The answer comes
    /// back through ProviderReady, so nothing is retained here; an Initialize that throws is the
    /// SDK refusing, and that slot is Failed on the spot.
    /// </summary>
    internal class InitializeProvidersCommand : Command
    {
        [Inject] private IAnalyticsModel _model { get; set; }
        [InjectSignal] private AnalyticsInternalSignals _signals { get; set; }

        public override void Execute()
        {
            if (!_model.IsLaunched)
                return;

            foreach (ProviderSlotVO slot in _model.Slots)
            {
                if (slot.State != AnalyticsProviderState.Plugged)
                    continue;

                slot.State = AnalyticsProviderState.Initializing;
                IAnalyticsProvider provider = slot.Provider;
                FlowLogger.Log($"Initialize - {provider.Name}");

                try
                {
                    provider.Initialize(ready => _signals.ProviderReady.Dispatch(provider, ready));
                }
                catch (Exception exception)
                {
                    slot.State = AnalyticsProviderState.Failed;
                    FlowLogger.LogError($"Initialize - '{provider.Name}' threw: {exception.Message}");
                }
            }
        }
    }
}
