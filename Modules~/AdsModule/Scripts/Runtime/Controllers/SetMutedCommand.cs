using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AdsModule.Models;
using Modules.AdsModule.Services;

namespace Modules.AdsModule.Controllers
{
    /// <summary>Kept as the current value and applied at once to a plugged provider; one plugged later gets it when it is ready.</summary>
    internal class SetMutedCommand : Command
    {
        [Inject] private IAdsModel _model { get; set; }

        [SignalParam] private bool _muted { get; set; }

        public override void Execute()
        {
            _model.SetMuted(_muted);
            FlowLogger.Log($"Muted - {_muted}");

            IAdsProvider provider = _model.Provider;

            if (provider == null)
                return;

            try
            {
                provider.SetMuted(_muted);
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"Muted - '{provider.Name}' threw: {exception.Message}");
            }
        }
    }
}
