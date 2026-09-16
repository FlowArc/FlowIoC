using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AdsModule.Data.ValueObjects;
using Modules.AdsModule.Models;
using Modules.AdsModule.Shared.Enums;
using Modules.AdsModule.Signals;

namespace Modules.AdsModule.Controllers
{
    /// <summary>The ad is on screen: the show is marked displayed, so the timeout stays quiet, and the game hears Opened.</summary>
    internal class AnnounceAdOpenedCommand : Command
    {
        [Inject] private IAdsModel _model { get; set; }
        [InjectSignal] private AdsSignals _ads { get; set; }

        [SignalParam] private AdFormat _format { get; set; }

        public override void Execute()
        {
            ShowVO current = _model.Current;

            if (current == null || current.Format != _format)
            {
                FlowLogger.Log($"Opened - {_format} with no show in progress; ignored.");
                return;
            }

            current.Displayed = true;
            FlowLogger.Log($"Opened - {current}");
            _ads.Outgoing.Opened.Dispatch(_format, current.Placement);
        }
    }
}
