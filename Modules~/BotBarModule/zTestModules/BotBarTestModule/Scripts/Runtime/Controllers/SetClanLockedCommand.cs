#if UNITY_EDITOR
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.BotBarModule.Signals;

namespace Modules.BotBarModule.BotBarTestModule.Controllers
{
    /// <summary>The Lock clan and Unlock clan buttons: the decision a game's progression would announce.</summary>
    internal class SetClanLockedCommand : Command
    {
        [InjectSignal] private BotBarSignals _botBar { get; set; }

        [SignalParam] private bool _locked { get; set; }

        public override void Execute() => _botBar.Incoming.SetLocked.Dispatch("clan", _locked);
    }
}
#endif
