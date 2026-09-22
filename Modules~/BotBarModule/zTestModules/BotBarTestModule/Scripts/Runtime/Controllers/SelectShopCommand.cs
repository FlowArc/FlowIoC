#if UNITY_EDITOR
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.BotBarModule.Signals;

namespace Modules.BotBarModule.BotBarTestModule.Controllers
{
    /// <summary>The Select shop button: a programmatic selection by key, the way a "go to the shop" offer would ask.</summary>
    internal class SelectShopCommand : Command
    {
        [InjectSignal] private BotBarSignals _botBar { get; set; }

        public override void Execute() => _botBar.Incoming.SelectTab.Dispatch("shop");
    }
}
#endif
