using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AdsModule.Models;
using Modules.AdsModule.Services;

namespace Modules.AdsModule.Controllers
{
    /// <summary>
    /// One provider, because one mediation SDK is what a game installs. A second plug is refused
    /// with an error naming both, and the first stays; the fix is one entry on AdsServiceRoot.
    /// </summary>
    internal class PlugProviderCommand : Command
    {
        [Inject] private IAdsModel _model { get; set; }

        [SignalParam] private IAdsProvider _provider { get; set; }

        public override void Execute()
        {
            if (_provider == null)
            {
                FlowLogger.LogError("Plug - a null provider was handed in; nothing plugged.");
                return;
            }

            if (_model.Provider != null)
            {
                FlowLogger.LogError(
                    $"Plug - '{_model.Provider.Name}' is already plugged; '{_provider.Name}' refused. One mediation SDK per game: list one plug on AdsServiceRoot.");
                return;
            }

            _model.Plug(_provider);
            FlowLogger.Log($"Plug - {_provider.Name}");
        }
    }
}
