using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AdsModule.Models;
using Modules.AdsModule.Services;

namespace Modules.AdsModule.Controllers
{
    /// <summary>The provider's own debug screen, for the Device Debugger's row.</summary>
    internal class ShowDebuggerCommand : Command
    {
        [Inject] private IAdsModel _model { get; set; }

        public override void Execute()
        {
            IAdsProvider provider = _model.Provider;

            if (provider == null)
            {
                FlowLogger.Log("Debugger - no provider plugged; nothing to show.");
                return;
            }

            FlowLogger.Log($"Debugger - {provider.Name}");

            try
            {
                provider.ShowDebugger();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"Debugger - '{provider.Name}' threw: {exception.Message}");
            }
        }
    }
}
