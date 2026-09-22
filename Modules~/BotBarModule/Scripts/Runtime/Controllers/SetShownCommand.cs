using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.BotBarModule.Models;
using Modules.BotBarModule.Signals;

namespace Modules.BotBarModule.Controllers
{
    /// <summary>
    /// Bound twice, with true on Show and false on Hide. With the screen not yet open only the
    /// Model changes, and the opening Command reads it.
    /// </summary>
    internal class SetShownCommand : Command<bool>
    {
        [Inject] private IBotBarModel _model { get; set; }
        [InjectSignal] private BotBarSignals _signals { get; set; }

        public override void Execute(bool shown)
        {
            if (!_model.SetShown(shown))
                return;

            if (shown)
                _signals.Outgoing.Shown.Dispatch();
            else
                _signals.Outgoing.Hidden.Dispatch();

            FlowLogger.Log($"Execute - SetShownCommand | {shown}");
        }
    }
}
