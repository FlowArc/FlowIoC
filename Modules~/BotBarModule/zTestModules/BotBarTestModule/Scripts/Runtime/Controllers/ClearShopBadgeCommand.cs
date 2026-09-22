#if UNITY_EDITOR
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.BotBarModule.BotBarTestModule.Models;
using Modules.BotBarModule.Signals;

namespace Modules.BotBarModule.BotBarTestModule.Controllers
{
    /// <summary>The Badge shop clear button.</summary>
    internal class ClearShopBadgeCommand : Command
    {
        [Inject] private BotBarTestModel _model { get; set; }
        [InjectSignal] private BotBarSignals _botBar { get; set; }

        public override void Execute()
        {
            _model.ShopBadge = 0;
            _botBar.Incoming.SetBadge.Dispatch("shop", 0);
        }
    }
}
#endif
