using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AnalyticsModule.Data.ValueObjects;
using Modules.AnalyticsModule.Enums;
using Modules.AnalyticsModule.Models;

namespace Modules.AnalyticsModule.Controllers
{
    /// <summary>The value is kept as current - the latest wins - and goes to every Ready slot now; a slot that becomes ready later reads it at its flush.</summary>
    internal class SetUserPropertyCommand : Command
    {
        [Inject] private IAnalyticsModel _model { get; set; }

        [SignalParam(0)] private string _name { get; set; }
        [SignalParam(1)] private string _value { get; set; }

        public override void Execute()
        {
            if (string.IsNullOrEmpty(_name))
            {
                FlowLogger.LogError("SetUserProperty - a property with no name was handed in; nothing set.");
                return;
            }

            string value = _value ?? string.Empty;
            _model.SetUserProperty(_name, value);
            FlowLogger.Log($"SetUserProperty - {_name}={value}");

            foreach (ProviderSlotVO slot in _model.Slots)
            {
                if (slot.State != AnalyticsProviderState.Ready)
                    continue;

                try
                {
                    slot.Provider.SetUserProperty(_name, value);
                }
                catch (Exception exception)
                {
                    FlowLogger.LogError($"SetUserProperty - '{slot.Provider.Name}' threw on '{_name}': {exception.Message}");
                }
            }
        }
    }
}
