using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AdsModule.Enums;
using Modules.AdsModule.Models;
using Modules.AdsModule.Services;

namespace Modules.AdsModule.Controllers
{
    /// <summary>
    /// Asks the plugged provider to bring its SDK up, once initialize has been requested and the
    /// provider is still Plugged. Consent already held goes first, because MAX wants it before
    /// InitializeSdk. The answer comes back through the listener, so nothing is retained here;
    /// an Initialize that throws is the SDK refusing, and the provider is Failed on the spot.
    /// </summary>
    internal class InitializeProviderCommand : Command
    {
        [Inject] private IAdsModel _model { get; set; }
        [Inject] private IAdsProviderListener _listener { get; set; }

        public override void Execute()
        {
            IAdsProvider provider = _model.Provider;

            if (!_model.IsInitializeRequested || provider == null || _model.ProviderState != AdsProviderState.Plugged)
                return;

            _model.SetProviderState(AdsProviderState.Initializing);
            FlowLogger.Log($"Initialize - {provider.Name}");

            try
            {
                if (_model.Consent.HasValue)
                    provider.SetConsent(_model.Consent.Value);

                provider.Initialize(_listener);
            }
            catch (Exception exception)
            {
                _model.SetProviderState(AdsProviderState.Failed);
                FlowLogger.LogError($"Initialize - '{provider.Name}' threw: {exception.Message}");
            }
        }
    }
}
