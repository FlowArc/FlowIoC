using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AdsModule.Data.ValueObjects;
using Modules.AdsModule.Enums;
using Modules.AdsModule.Models;
using Modules.AdsModule.Services;

namespace Modules.AdsModule.Controllers
{
    /// <summary>
    /// Forgets the provider. A show still in progress is answered NotReady here, because its
    /// Closed will never come from a provider that is gone.
    /// </summary>
    internal class UnplugProviderCommand : Command
    {
        [Inject] private IAdsModel _model { get; set; }

        [SignalParam] private IAdsProvider _provider { get; set; }

        public override void Execute()
        {
            if (_provider == null || _model.Provider != _provider)
            {
                FlowLogger.Log($"Unplug - '{_provider?.Name ?? "null"}' is not the plugged provider; nothing to unplug.");
                return;
            }

            ShowVO current = _model.Current;
            _model.Unplug();
            FlowLogger.Log($"Unplug - {_provider.Name}");

            if (current == null) return;

            var result = new AdResultVO(current.Format, current.Placement, AdOutcome.NotReady, "provider unplugged");
            FlowLogger.Log($"Unplug - {result}");

            try
            {
                current.Done?.Invoke(result);
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"Unplug - the callback for '{current.Placement}' threw: {exception}");
            }
        }
    }
}
