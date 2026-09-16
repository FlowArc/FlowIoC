using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AdsModule.Data.ValueObjects;
using Modules.AdsModule.Models;
using Modules.AdsModule.Services;

namespace Modules.AdsModule.Controllers
{
    /// <summary>Kept as the current value and applied at once to a plugged provider, whatever its state - before Initialize is where MAX wants it.</summary>
    internal class SetConsentCommand : Command
    {
        [Inject] private IAdsModel _model { get; set; }

        [SignalParam] private AdsConsentVO _consent { get; set; }

        public override void Execute()
        {
            _model.SetConsent(_consent);
            FlowLogger.Log($"Consent - {_consent}");

            IAdsProvider provider = _model.Provider;

            if (provider == null)
                return;

            try
            {
                provider.SetConsent(_consent);
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"Consent - '{provider.Name}' threw: {exception.Message}");
            }
        }
    }
}
