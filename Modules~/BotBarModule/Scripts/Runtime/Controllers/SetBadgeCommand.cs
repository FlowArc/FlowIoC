using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.BotBarModule.Models;
using Modules.BotBarModule.Signals;

namespace Modules.BotBarModule.Controllers
{
    /// <summary>A count on a tab; 0 clears. Announced only when it changed, clamped at 0.</summary>
    internal class SetBadgeCommand : Command
    {
        [Inject] private IBotBarModel _model { get; set; }
        [InjectSignal] private BotBarSignals _signals { get; set; }

        [SignalParam] private string _key { get; set; }
        [SignalParam] private int _count { get; set; }

        public override void Execute()
        {
            if (!_model.HasTab(_key))
            {
                FlowLogger.LogWarning($"SetBadge - no tab keyed '{_key}' in CD_BotBar.");
                return;
            }

            if (!_model.SetBadge(_key, _count))
                return;

            int badge = _model.GetTab(_key).Badge;
            _signals.Outgoing.BadgeChanged.Dispatch(_key, badge);
            FlowLogger.Log($"Execute - SetBadgeCommand | {_key} {badge}");
        }
    }
}
