#if UNITY_EDITOR
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.BotBarModule.BotBarTestModule.Models;
using Modules.BotBarModule.BotBarTestModule.Signals;

namespace Modules.BotBarModule.BotBarTestModule.Controllers
{
    /// <summary>Remembers the last announcement heard and puts it on the label.</summary>
    internal class NoteCommand : Command
    {
        [Inject] private BotBarTestModel _model { get; set; }
        [InjectSignal] private BotBarTestInternalSignals _signals { get; set; }

        [SignalParam] private string _line { get; set; }

        public override void Execute()
        {
            _model.LastNote = _line;
            _signals.StatusChanged.Dispatch($"{_line}   (shop badge {_model.ShopBadge})");
        }
    }
}
#endif
