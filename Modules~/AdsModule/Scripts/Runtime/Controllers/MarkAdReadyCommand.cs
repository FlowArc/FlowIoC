using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AdsModule.Data.ValueObjects;
using Modules.AdsModule.Enums;
using Modules.AdsModule.Models;
using Modules.AdsModule.Shared.Enums;
using Modules.AdsModule.Signals;

namespace Modules.AdsModule.Controllers
{
    /// <summary>A load landed: the slot is Ready, the attempt count starts over, and the game hears ReadyChanged.</summary>
    internal class MarkAdReadyCommand : Command
    {
        [Inject] private IAdsModel _model { get; set; }
        [InjectSignal] private AdsSignals _ads { get; set; }

        [SignalParam] private AdFormat _format { get; set; }

        public override void Execute()
        {
            AdSlotVO slot = _model.GetSlot(_format);
            slot.State = AdLoadState.Ready;
            slot.Attempt = 0;
            FlowLogger.Log($"Loaded - {_format}");
            _ads.Outgoing.ReadyChanged.Dispatch(_format, true);
        }
    }
}
