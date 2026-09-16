using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AnalyticsModule.Data.ValueObjects;
using Modules.AnalyticsModule.Enums;
using Modules.AnalyticsModule.Models;

namespace Modules.AnalyticsModule.Controllers
{
    /// <summary>The consent is kept as current and goes to every Ready slot now; a slot that becomes ready later reads it first at its flush.</summary>
    internal class SetConsentCommand : Command
    {
        [Inject] private IAnalyticsModel _model { get; set; }

        [SignalParam] private AnalyticsConsentVO _consent { get; set; }

        public override void Execute()
        {
            _model.SetConsent(_consent);
            FlowLogger.Log($"SetConsent - {_consent}");

            foreach (ProviderSlotVO slot in _model.Slots)
            {
                if (slot.State != AnalyticsProviderState.Ready)
                    continue;

                try
                {
                    slot.Provider.SetConsent(_consent);
                }
                catch (Exception exception)
                {
                    FlowLogger.LogError($"SetConsent - '{slot.Provider.Name}' threw: {exception.Message}");
                }
            }
        }
    }
}
