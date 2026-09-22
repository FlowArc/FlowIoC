#if UNITY_EDITOR
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.BotBarModule.BotBarTestModule.Models;
using Modules.BotBarModule.Signals;

namespace Modules.BotBarModule.BotBarTestModule.Controllers
{
    /// <summary>The Badge shop +1 button.</summary>
    internal class IncrementShopBadgeCommand : Command
    {
        [Inject] private BotBarTestModel _model { get; set; }
        [InjectSignal] private BotBarSignals _botBar { get; set; }

        public override void Execute()
        {
            _model.ShopBadge++;
            _botBar.Incoming.SetBadge.Dispatch("shop", _model.ShopBadge);
        }
    }
}
#endif
