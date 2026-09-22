#if UNITY_EDITOR
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.BotBarModule.Signals;

namespace Modules.BotBarModule.BotBarTestModule.Controllers
{
    /// <summary>The Hide and Show buttons: what a popup opening and closing would ask.</summary>
    internal class SetBarShownCommand : Command
    {
        [InjectSignal] private BotBarSignals _botBar { get; set; }

        [SignalParam] private bool _shown { get; set; }

        public override void Execute()
        {
            if (_shown)
                _botBar.Incoming.Show.Dispatch();
            else
                _botBar.Incoming.Hide.Dispatch();
        }
    }
}
#endif
