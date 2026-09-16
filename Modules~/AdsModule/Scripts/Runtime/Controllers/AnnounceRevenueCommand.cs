using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AdsModule.Shared.Data.ValueObjects;
using Modules.AdsModule.Signals;

namespace Modules.AdsModule.Controllers
{
    /// <summary>What the impression paid, handed to the game as it came.</summary>
    internal class AnnounceRevenueCommand : Command
    {
        [InjectSignal] private AdsSignals _ads { get; set; }

        [SignalParam] private AdRevenueVO _revenue { get; set; }

        public override void Execute()
        {
            FlowLogger.Log($"Revenue - {_revenue}");
            _ads.Outgoing.RevenuePaid.Dispatch(_revenue);
        }
    }
}
