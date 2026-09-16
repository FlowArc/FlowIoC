#if UNITY_EDITOR

using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AdsModule.AdsTestModule.Services;

namespace Modules.AdsModule.AdsTestModule.Controllers
{
    /// <summary>Revenue: the fake reports what the ad on screen paid.</summary>
    public class PayRevenueCommand : Command
    {
        [Inject] private FakeAdsProvider _provider { get; set; }

        public override void Execute() => _provider.PayRevenue();
    }
}

#endif
