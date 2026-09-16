using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AdsModule.Enums;
using Modules.AdsModule.Models;

namespace Modules.AdsModule.Controllers
{
    /// <summary>
    /// The game's own Initialize. A provider that failed is given another go - the state goes
    /// back to Plugged so the step after this one asks it again.
    /// </summary>
    internal class RequestInitializeCommand : Command
    {
        [Inject] private IAdsModel _model { get; set; }

        public override void Execute()
        {
            if (_model.ProviderState == AdsProviderState.Failed)
            {
                FlowLogger.Log($"Initialize - '{_model.Provider.Name}' failed before; trying again.");
                _model.SetProviderState(AdsProviderState.Plugged);
            }
            else if (_model.IsInitializeRequested)
            {
                FlowLogger.Log("Initialize - already requested.");
            }

            _model.RequestInitialize();
        }
    }
}
